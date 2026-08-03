using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Common;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-returns")]
[Authorize]
public sealed class PettyCashReturnsController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record ReturnLineRequest(Guid PettyCashRequestLineId, decimal Amount);
    public sealed record CreateReturnRequest(Guid PettyCashFundId, string? PreparedByName, string? Notes, IReadOnlyList<ReturnLineRequest> Lines);
    public sealed record ReceiveReturnRequest(DateTimeOffset? ReceivedAt, string ReceiptReference);
    public sealed record RejectReturnRequest(string Reason);

    public sealed record ReturnSummaryDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string PreparedByName,
        DateTimeOffset PreparedAt,
        PettyCashReturnStatus Status,
        int LineCount,
        decimal TotalAmount,
        string? ReceiptReference);

    public sealed record ReturnLineDto(
        Guid Id,
        Guid PettyCashRequestLineId,
        Guid PettyCashRequestId,
        string RequestNumber,
        PettyCashRequestCategory Category,
        string? ServiceJobNumber,
        string? CustomCategoryName,
        string Purpose,
        decimal Amount);

    public sealed record ReturnDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string PreparedByName,
        DateTimeOffset PreparedAt,
        string? Notes,
        PettyCashReturnStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ReceivedAt,
        string? ReceiptReference,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        DateTimeOffset? CancelledAt,
        decimal TotalAmount,
        IReadOnlyList<ReturnLineDto> Lines);

    public sealed record ReturnCandidateDto(
        Guid PettyCashRequestLineId,
        Guid PettyCashFundId,
        string PettyCashFundCode,
        string RequestNumber,
        PettyCashRequestCategory Category,
        string? ServiceJobNumber,
        string? CustomCategoryName,
        string Purpose,
        decimal FundedAmount,
        decimal LedgerBalance,
        decimal PendingReturnAmount,
        decimal PendingReallocationAmount,
        decimal AvailableToReturn,
        int OpenIouCount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReturnSummaryDto>>> List(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnView, cancellationToken)) return Forbid();

        var rows = await dbContext.PettyCashReturns.AsNoTracking()
            .OrderByDescending(x => x.PreparedAt)
            .Select(x => new ReturnSummaryDto(
                x.Id,
                x.Number,
                x.PettyCashFundId,
                dbContext.PettyCashFunds.Where(f => f.Id == x.PettyCashFundId).Select(f => f.Code).FirstOrDefault(),
                x.PreparedByName,
                x.PreparedAt,
                x.Status,
                x.Lines.Count,
                x.Lines.Sum(line => line.Amount),
                x.ReceiptReference))
            .Take(500)
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("candidates")]
    public async Task<ActionResult<IReadOnlyList<ReturnCandidateDto>>> Candidates(
        [FromQuery] Guid? pettyCashFundId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnCreate, cancellationToken)) return Forbid();

        var query = dbContext.PettyCashRequests.AsNoTracking()
            .Where(request => request.Status == PettyCashRequestStatus.PartiallyFunded
                              || request.Status == PettyCashRequestStatus.Funded);
        if (pettyCashFundId is { } fundId) query = query.Where(request => request.PettyCashFundId == fundId);

        var rows = await query
            .SelectMany(request => request.Lines.Select(line => new
            {
                request.Number,
                request.PettyCashFundId,
                Line = line,
                FundedAmount = line.Fundings.Sum(funding => funding.Amount),
                FundCode = dbContext.PettyCashFunds.Where(fund => fund.Id == request.PettyCashFundId).Select(fund => fund.Code).First(),
            }))
            .Where(x => x.FundedAmount > 0m)
            .ToListAsync(cancellationToken);

        var lineIds = rows.Select(x => x.Line.Id).ToList();
        var balances = await dbContext.PettyCashFunds.AsNoTracking()
            .SelectMany(fund => fund.Transactions)
            .Where(transaction => transaction.PettyCashRequestLineId != null
                                  && lineIds.Contains(transaction.PettyCashRequestLineId.Value))
            .GroupBy(transaction => transaction.PettyCashRequestLineId!.Value)
            .Select(group => new
            {
                LineId = group.Key,
                Balance = group.Sum(transaction => transaction.Direction == PettyCashTransactionDirection.In
                    ? transaction.Amount
                    : -transaction.Amount),
            })
            .ToDictionaryAsync(x => x.LineId, x => x.Balance, cancellationToken);

        var reservations = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Status == PettyCashReturnStatus.Submitted)
            .SelectMany(x => x.Lines)
            .Where(x => lineIds.Contains(x.PettyCashRequestLineId))
            .GroupBy(x => x.PettyCashRequestLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, cancellationToken);

        var reallocationReservations = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Status == PettyCashReallocationStatus.Submitted
                        && lineIds.Contains(x.SourcePettyCashRequestLineId))
            .GroupBy(x => x.SourcePettyCashRequestLineId)
            .Select(group => new { LineId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, cancellationToken);

        var openIous = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.PettyCashRequestLineId != null
                        && lineIds.Contains(x.PettyCashRequestLineId.Value)
                        && (x.Status == PettyCashIouStatus.Released || x.Status == PettyCashIouStatus.Settled))
            .GroupBy(x => x.PettyCashRequestLineId!.Value)
            .Select(group => new { LineId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.LineId, x => x.Count, cancellationToken);

        var jobIds = rows.Where(x => x.Line.ServiceJobId != null).Select(x => x.Line.ServiceJobId!.Value).Distinct().ToList();
        var jobNumbers = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        return Ok(rows.Select(x =>
        {
            var balance = balances.GetValueOrDefault(x.Line.Id);
            var reserved = reservations.GetValueOrDefault(x.Line.Id);
            var reservedForReallocation = reallocationReservations.GetValueOrDefault(x.Line.Id);
            return new ReturnCandidateDto(
                x.Line.Id,
                x.PettyCashFundId,
                x.FundCode,
                x.Number,
                x.Line.Category,
                x.Line.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null,
                x.Line.CustomCategoryName,
                x.Line.Purpose,
                x.FundedAmount,
                balance,
                reserved,
                reservedForReallocation,
                Math.Max(0m, balance - reserved - reservedForReallocation),
                openIous.GetValueOrDefault(x.Line.Id));
        }).Where(x => x.AvailableToReturn > 0m).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReturnDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnView, cancellationToken)) return Forbid();

        var pettyCashReturn = await dbContext.PettyCashReturns.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pettyCashReturn is null) return NotFound();

        var fundCode = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == pettyCashReturn.PettyCashFundId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var lineIds = pettyCashReturn.Lines.Select(x => x.PettyCashRequestLineId).ToList();
        var categories = await dbContext.PettyCashRequests.AsNoTracking()
            .SelectMany(request => request.Lines.Select(line => new
            {
                RequestId = request.Id,
                request.Number,
                Line = line,
            }))
            .Where(x => lineIds.Contains(x.Line.Id))
            .ToListAsync(cancellationToken);
        var jobIds = categories.Where(x => x.Line.ServiceJobId != null).Select(x => x.Line.ServiceJobId!.Value).Distinct().ToList();
        var jobNumbers = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        return Ok(new ReturnDto(
            pettyCashReturn.Id,
            pettyCashReturn.Number,
            pettyCashReturn.PettyCashFundId,
            fundCode,
            pettyCashReturn.PreparedByName,
            pettyCashReturn.PreparedAt,
            pettyCashReturn.Notes,
            pettyCashReturn.Status,
            pettyCashReturn.SubmittedAt,
            pettyCashReturn.ReceivedAt,
            pettyCashReturn.ReceiptReference,
            pettyCashReturn.RejectedAt,
            pettyCashReturn.RejectionReason,
            pettyCashReturn.CancelledAt,
            pettyCashReturn.TotalAmount,
            pettyCashReturn.Lines.Select(line =>
            {
                var category = categories.Single(x => x.Line.Id == line.PettyCashRequestLineId);
                return new ReturnLineDto(
                    line.Id,
                    line.PettyCashRequestLineId,
                    category.RequestId,
                    category.Number,
                    category.Line.Category,
                    category.Line.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null,
                    category.Line.CustomCategoryName,
                    category.Line.Purpose,
                    line.Amount);
            }).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ReturnDto>> Create(CreateReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnCreate, cancellationToken)) return Forbid();
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new DomainValidationException("Select at least one funded category to return.");
        }
        if (request.Lines.GroupBy(x => x.PettyCashRequestLineId).Any(group => group.Count() > 1))
        {
            throw new DomainValidationException("Each funded category can appear only once on a petty cash return.");
        }

        var preparedByName = string.IsNullOrWhiteSpace(request.PreparedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.PreparedByName;
        var id = await financeService.CreatePettyCashReturnAsync(
            request.PettyCashFundId,
            currentUser.UserId ?? Guid.Empty,
            preparedByName,
            request.Notes,
            request.Lines.ToDictionary(x => x.PettyCashRequestLineId, x => x.Amount),
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnSubmit, cancellationToken)) return Forbid();
        await financeService.SubmitPettyCashReturnAsync(id, cancellationToken);
        await NotifyHeadOfficeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult> Receive(Guid id, ReceiveReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnReceive, cancellationToken)) return Forbid();
        await financeService.ReceivePettyCashReturnAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            request.ReceivedAt?.ToUniversalTime(),
            request.ReceiptReference,
            cancellationToken);
        await NotifyPreparerAsync(id, "Petty cash return received", "Head office confirmed receipt and the category balances were reduced.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, RejectReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnReject, cancellationToken)) return Forbid();
        await financeService.RejectPettyCashReturnAsync(id, currentUser.UserId ?? Guid.Empty, request.Reason, cancellationToken);
        await NotifyPreparerAsync(id, "Petty cash return rejected", $"Head office rejected it. {request.Reason.Trim()}", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnCancel, cancellationToken)) return Forbid();
        await financeService.CancelPettyCashReturnAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task NotifyHeadOfficeAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.PreparedByName, Total = x.Lines.Sum(line => line.Amount) })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return;

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashReturnReceive, AppPermissions.PettyCashReturnReject],
            currentUser.UserId,
            cancellationToken);
        notificationService.EnqueueInAppForUsers(
            recipients,
            "Petty cash return awaiting receipt",
            $"{row.Number} from {row.PreparedByName} returns {row.Total:0.00}. Count the cash and record the receipt reference.",
            $"/finance/petty-cash-returns/{row.Id}",
            ReferenceTypes.PettyCashReturn,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyPreparerAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashReturns.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.PreparedByUserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null || row.PreparedByUserId == Guid.Empty) return;

        notificationService.EnqueueInApp(
            row.PreparedByUserId,
            title,
            $"{row.Number}: {message}",
            $"/finance/petty-cash-returns/{row.Id}",
            ReferenceTypes.PettyCashReturn,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
}

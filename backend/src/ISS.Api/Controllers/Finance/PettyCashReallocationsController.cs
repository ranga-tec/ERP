using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-reallocations")]
[Authorize]
public sealed class PettyCashReallocationsController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record CreateReallocationRequest(
        Guid PettyCashFundId,
        Guid SourcePettyCashRequestLineId,
        Guid DestinationPettyCashRequestLineId,
        decimal Amount,
        string Reason,
        string? RequestedByName);

    public sealed record RejectReallocationRequest(string Reason);

    public sealed record CategoryReferenceDto(
        Guid PettyCashRequestLineId,
        Guid PettyCashRequestId,
        string RequestNumber,
        PettyCashRequestCategory Category,
        string? ServiceJobNumber,
        string? CustomCategoryName,
        string Purpose);

    public sealed record ReallocationCandidateDto(
        Guid PettyCashRequestLineId,
        Guid PettyCashRequestId,
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
        decimal AvailableBalance);

    public sealed record ReallocationSummaryDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        PettyCashReallocationStatus Status,
        decimal Amount,
        CategoryReferenceDto Source,
        CategoryReferenceDto Destination);

    public sealed record ReallocationDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        string Reason,
        PettyCashReallocationStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        DateTimeOffset? CancelledAt,
        decimal Amount,
        decimal SourceBalance,
        decimal DestinationBalance,
        CategoryReferenceDto Source,
        CategoryReferenceDto Destination);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReallocationSummaryDto>>> List(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationView, cancellationToken)) return Forbid();

        var rows = await dbContext.PettyCashReallocations.AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var fundIds = rows.Select(x => x.PettyCashFundId).Distinct().ToList();
        var fundCodes = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => fundIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var lineIds = rows.SelectMany(x => new[]
        {
            x.SourcePettyCashRequestLineId,
            x.DestinationPettyCashRequestLineId,
        }).Distinct().ToList();
        var categories = await LoadCategoryReferencesAsync(lineIds, cancellationToken);

        return Ok(rows.Select(x => new ReallocationSummaryDto(
            x.Id,
            x.Number,
            x.PettyCashFundId,
            fundCodes.GetValueOrDefault(x.PettyCashFundId),
            x.RequestedByName,
            x.RequestedAt,
            x.Status,
            x.Amount,
            categories[x.SourcePettyCashRequestLineId],
            categories[x.DestinationPettyCashRequestLineId])).ToList());
    }

    [HttpGet("candidates")]
    public async Task<ActionResult<IReadOnlyList<ReallocationCandidateDto>>> Candidates(
        [FromQuery] Guid? pettyCashFundId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationCreate, cancellationToken)) return Forbid();

        var query = dbContext.PettyCashRequests.AsNoTracking()
            .Where(request => request.Status == PettyCashRequestStatus.PartiallyFunded
                              || request.Status == PettyCashRequestStatus.Funded);
        if (pettyCashFundId is { } fundId) query = query.Where(request => request.PettyCashFundId == fundId);

        var rows = await query
            .SelectMany(request => request.Lines.Select(line => new
            {
                RequestId = request.Id,
                request.Number,
                request.PettyCashFundId,
                Line = line,
                FundedAmount = line.Fundings.Sum(funding => funding.Amount),
                FundCode = dbContext.PettyCashFunds
                    .Where(fund => fund.Id == request.PettyCashFundId && fund.IsActive)
                    .Select(fund => fund.Code)
                    .FirstOrDefault(),
            }))
            .Where(x => x.FundedAmount > 0m && x.FundCode != null)
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
        var returnReservations = await dbContext.PettyCashReturns.AsNoTracking()
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

        var jobIds = rows.Where(x => x.Line.ServiceJobId != null)
            .Select(x => x.Line.ServiceJobId!.Value)
            .Distinct()
            .ToList();
        var jobNumbers = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        return Ok(rows.Select(x =>
        {
            var balance = balances.GetValueOrDefault(x.Line.Id);
            var pendingReturn = returnReservations.GetValueOrDefault(x.Line.Id);
            var pendingReallocation = reallocationReservations.GetValueOrDefault(x.Line.Id);
            return new ReallocationCandidateDto(
                x.Line.Id,
                x.RequestId,
                x.PettyCashFundId,
                x.FundCode!,
                x.Number,
                x.Line.Category,
                x.Line.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null,
                x.Line.CustomCategoryName,
                x.Line.Purpose,
                x.FundedAmount,
                balance,
                pendingReturn,
                pendingReallocation,
                Math.Max(0m, balance - pendingReturn - pendingReallocation));
        }).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReallocationDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationView, cancellationToken)) return Forbid();

        var reallocation = await dbContext.PettyCashReallocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (reallocation is null) return NotFound();

        var fundCode = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == reallocation.PettyCashFundId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);
        var lineIds = new[]
        {
            reallocation.SourcePettyCashRequestLineId,
            reallocation.DestinationPettyCashRequestLineId,
        };
        var categories = await LoadCategoryReferencesAsync(lineIds, cancellationToken);
        if (categories.Count != 2) return NotFound();

        var balances = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == reallocation.PettyCashFundId)
            .SelectMany(x => x.Transactions)
            .Where(x => x.PettyCashRequestLineId != null && lineIds.Contains(x.PettyCashRequestLineId.Value))
            .GroupBy(x => x.PettyCashRequestLineId!.Value)
            .Select(group => new
            {
                LineId = group.Key,
                Balance = group.Sum(x => x.Direction == PettyCashTransactionDirection.In ? x.Amount : -x.Amount),
            })
            .ToDictionaryAsync(x => x.LineId, x => x.Balance, cancellationToken);

        return Ok(new ReallocationDto(
            reallocation.Id,
            reallocation.Number,
            reallocation.PettyCashFundId,
            fundCode,
            reallocation.RequestedByName,
            reallocation.RequestedAt,
            reallocation.Reason,
            reallocation.Status,
            reallocation.SubmittedAt,
            reallocation.ApprovedAt,
            reallocation.RejectedAt,
            reallocation.RejectionReason,
            reallocation.CancelledAt,
            reallocation.Amount,
            balances.GetValueOrDefault(reallocation.SourcePettyCashRequestLineId),
            balances.GetValueOrDefault(reallocation.DestinationPettyCashRequestLineId),
            categories[reallocation.SourcePettyCashRequestLineId],
            categories[reallocation.DestinationPettyCashRequestLineId]));
    }

    [HttpPost]
    public async Task<ActionResult<ReallocationDto>> Create(
        CreateReallocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationCreate, cancellationToken)) return Forbid();

        var requestedByName = string.IsNullOrWhiteSpace(request.RequestedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.RequestedByName;
        var id = await financeService.CreatePettyCashReallocationAsync(
            request.PettyCashFundId,
            request.SourcePettyCashRequestLineId,
            request.DestinationPettyCashRequestLineId,
            request.Amount,
            request.Reason,
            currentUser.UserId ?? Guid.Empty,
            requestedByName,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationSubmit, cancellationToken)) return Forbid();
        await financeService.SubmitPettyCashReallocationAsync(id, cancellationToken);
        await NotifyHeadOfficeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationApprove, cancellationToken)) return Forbid();
        await financeService.ApprovePettyCashReallocationAsync(id, currentUser.UserId ?? Guid.Empty, cancellationToken);
        await NotifyRequesterAsync(
            id,
            "Petty cash reallocation approved",
            "Head office approved it. The source and destination category balances have been updated; physical cash did not move.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(
        Guid id,
        RejectReallocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationReject, cancellationToken)) return Forbid();
        await financeService.RejectPettyCashReallocationAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            request.Reason,
            cancellationToken);
        await NotifyRequesterAsync(
            id,
            "Petty cash reallocation rejected",
            $"Head office rejected it. {request.Reason.Trim()}",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReallocationCancel, cancellationToken)) return Forbid();
        await financeService.CancelPettyCashReallocationAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<Dictionary<Guid, CategoryReferenceDto>> LoadCategoryReferencesAsync(
        IReadOnlyCollection<Guid> lineIds,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PettyCashRequests.AsNoTracking()
            .SelectMany(request => request.Lines
                .Where(line => lineIds.Contains(line.Id))
                .Select(line => new
                {
                    LineId = line.Id,
                    RequestId = request.Id,
                    RequestNumber = request.Number,
                    line.Category,
                    line.ServiceJobId,
                    line.CustomCategoryName,
                    line.Purpose,
                }))
            .ToListAsync(cancellationToken);
        var jobIds = rows.Where(x => x.ServiceJobId != null)
            .Select(x => x.ServiceJobId!.Value)
            .Distinct()
            .ToList();
        var jobNumbers = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        return rows.ToDictionary(
            x => x.LineId,
            x => new CategoryReferenceDto(
                x.LineId,
                x.RequestId,
                x.RequestNumber,
                x.Category,
                x.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null,
                x.CustomCategoryName,
                x.Purpose));
    }

    private async Task NotifyHeadOfficeAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByName, x.Amount })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return;

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashReallocationApprove, AppPermissions.PettyCashReallocationReject],
            currentUser.UserId,
            cancellationToken);
        notificationService.EnqueueInAppForUsers(
            recipients,
            "Petty cash reallocation awaiting approval",
            $"{row.Number} from {row.RequestedByName} requests a category transfer of {row.Amount:0.00}.",
            $"/finance/petty-cash-reallocations/{row.Id}",
            ReferenceTypes.PettyCashReallocation,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyRequesterAsync(
        Guid id,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashReallocations.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByUserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null || row.RequestedByUserId == Guid.Empty) return;

        notificationService.EnqueueInApp(
            row.RequestedByUserId,
            title,
            $"{row.Number}: {message}",
            $"/finance/petty-cash-reallocations/{row.Id}",
            ReferenceTypes.PettyCashReallocation,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
}

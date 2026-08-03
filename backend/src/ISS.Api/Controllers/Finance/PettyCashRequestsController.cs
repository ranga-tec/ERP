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
[Route("api/finance/petty-cash-requests")]
[Authorize]
public sealed class PettyCashRequestsController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record PettyCashRequestLineFundingDto(
        Guid Id,
        decimal Amount,
        DateTimeOffset FundedAt,
        string? PaymentReference,
        string? Notes);

    public sealed record PettyCashRequestLineDto(
        Guid Id,
        PettyCashRequestCategory Category,
        Guid? ServiceJobId,
        string? ServiceJobNumber,
        string? CustomCategoryName,
        string Purpose,
        decimal RequestedAmount,
        decimal? ApprovedAmount,
        decimal FundedAmount,
        decimal OutstandingAmount,
        // What is left of this category in the custodian's float: funded in, spent out.
        decimal SubAccountBalance,
        IReadOnlyList<PettyCashRequestLineFundingDto> Fundings);

    public sealed record PettyCashRequestSummaryDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        DateTimeOffset? NeededByAt,
        PettyCashRequestStatus Status,
        int LineCount,
        decimal RequestedTotal,
        decimal ApprovedTotal,
        decimal FundedTotal,
        decimal OutstandingTotal);

    public sealed record PettyCashRequestDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        Guid RequestedByUserId,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        DateTimeOffset? NeededByAt,
        string? Notes,
        PettyCashRequestStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        Guid? ApprovedByUserId,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        decimal RequestedTotal,
        decimal ApprovedTotal,
        decimal FundedTotal,
        decimal OutstandingTotal,
        IReadOnlyList<PettyCashRequestLineDto> Lines);

    public sealed record CreatePettyCashRequestRequest(
        Guid PettyCashFundId,
        DateTimeOffset? NeededByAt,
        string? RequestedByName,
        string? Notes);

    public sealed record UpdatePettyCashRequestHeaderRequest(DateTimeOffset? NeededByAt, string? Notes);

    public sealed record PettyCashRequestLineRequest(
        PettyCashRequestCategory Category,
        Guid? ServiceJobId,
        string? CustomCategoryName,
        string Purpose,
        decimal RequestedAmount);

    public sealed record ApprovedLineAmount(Guid LineId, decimal ApprovedAmount);
    public sealed record ApprovePettyCashRequestRequest(IReadOnlyList<ApprovedLineAmount> Lines);
    public sealed record RejectPettyCashRequestRequest(string? Reason);

    public sealed record FundPettyCashRequestLineRequest(
        decimal Amount,
        DateTimeOffset? FundedAt,
        string? PaymentReference,
        string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PettyCashRequestSummaryDto>>> List(
        [FromQuery] Guid? pettyCashFundId,
        [FromQuery] PettyCashRequestStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var query = dbContext.PettyCashRequests.AsNoTracking();
        if (pettyCashFundId is not null)
        {
            query = query.Where(x => x.PettyCashFundId == pettyCashFundId.Value);
        }

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.Number)
            .Skip(skip)
            .Take(take)
            .Select(x => new PettyCashRequestSummaryDto(
                x.Id,
                x.Number,
                x.PettyCashFundId,
                dbContext.PettyCashFunds.Where(f => f.Id == x.PettyCashFundId).Select(f => f.Code).FirstOrDefault(),
                x.RequestedByName,
                x.RequestedAt,
                x.NeededByAt,
                x.Status,
                x.Lines.Count,
                x.Lines.Sum(l => l.RequestedAmount),
                x.Lines.Sum(l => l.ApprovedAmount ?? 0m),
                x.Lines.Sum(l => l.Fundings.Sum(f => f.Amount)),
                x.Lines.Sum(l => (l.ApprovedAmount ?? 0m) - l.Fundings.Sum(f => f.Amount))))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    /// <summary>
    /// The categories that actually hold money right now, for the forms that spend it. Flattened
    /// here rather than by walking every request on the client, which would be a request per row.
    /// </summary>
    [HttpGet("funded-lines")]
    public async Task<ActionResult<IReadOnlyList<FundedCategoryDto>>> FundedLines(
        [FromQuery] Guid? pettyCashFundId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestView, cancellationToken)
            && !await HasPermissionAsync(AppPermissions.PettyCashIouRelease, cancellationToken))
        {
            return Forbid();
        }

        var query = dbContext.PettyCashRequests.AsNoTracking()
            .Where(request => request.Status == PettyCashRequestStatus.PartiallyFunded
                              || request.Status == PettyCashRequestStatus.Funded);

        if (pettyCashFundId is not null)
        {
            query = query.Where(request => request.PettyCashFundId == pettyCashFundId.Value);
        }

        var rows = await query
            .SelectMany(request => request.Lines.Select(line => new
            {
                request.Number,
                request.PettyCashFundId,
                Line = line,
                Funded = line.Fundings.Sum(f => f.Amount),
            }))
            .Where(x => x.Funded > 0m)
            .ToListAsync(cancellationToken);

        var lineIds = rows.Select(x => x.Line.Id).ToList();
        var balanceByLineId = await dbContext.PettyCashFunds.AsNoTracking()
            .SelectMany(fund => fund.Transactions)
            .Where(transaction => transaction.PettyCashRequestLineId != null
                                  && lineIds.Contains(transaction.PettyCashRequestLineId.Value))
            .GroupBy(transaction => transaction.PettyCashRequestLineId!.Value)
            .Select(group => new
            {
                LineId = group.Key,
                Balance = group.Sum(transaction =>
                    transaction.Direction == PettyCashTransactionDirection.In
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

        var jobIds = rows
            .Where(x => x.Line.ServiceJobId != null)
            .Select(x => x.Line.ServiceJobId!.Value)
            .Distinct()
            .ToList();
        var jobNumberById = await dbContext.ServiceJobs.AsNoTracking()
            .Where(job => jobIds.Contains(job.Id))
            .ToDictionaryAsync(job => job.Id, job => job.Number, cancellationToken);

        var result = rows.Select(x => new FundedCategoryDto(
            x.Line.Id,
            x.Number,
            x.PettyCashFundId,
            x.Line.Category,
            x.Line.ServiceJobId,
            x.Line.ServiceJobId is { } jobId ? jobNumberById.GetValueOrDefault(jobId) : null,
            x.Line.CustomCategoryName,
            x.Line.Purpose,
            x.Funded,
            Math.Max(
                0m,
                balanceByLineId.GetValueOrDefault(x.Line.Id)
                - returnReservations.GetValueOrDefault(x.Line.Id)
                - reallocationReservations.GetValueOrDefault(x.Line.Id))))
            .ToList();

        return Ok(result);
    }

    public sealed record FundedCategoryDto(
        Guid Id,
        string RequestNumber,
        Guid PettyCashFundId,
        PettyCashRequestCategory Category,
        Guid? ServiceJobId,
        string? ServiceJobNumber,
        string? CustomCategoryName,
        string Purpose,
        decimal FundedAmount,
        decimal AvailableBalance);

    [HttpPost]
    public async Task<ActionResult<PettyCashRequestDto>> Create(
        CreatePettyCashRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestCreate, cancellationToken))
        {
            return Forbid();
        }

        var requestedByName = string.IsNullOrWhiteSpace(request.RequestedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.RequestedByName;

        var id = await financeService.CreatePettyCashRequestAsync(
            request.PettyCashFundId,
            currentUser.UserId ?? Guid.Empty,
            requestedByName,
            request.NeededByAt?.ToUniversalTime(),
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PettyCashRequestDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestView, cancellationToken))
        {
            return Forbid();
        }

        var request = await dbContext.PettyCashRequests.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(line => line.Fundings)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        var fundCode = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == request.PettyCashFundId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        // Sub-balances come from the fund ledger rather than the request, because money leaves a
        // category through advances and vouchers that the request itself never sees.
        var lineIds = request.Lines.Select(x => x.Id).ToList();
        var balanceByLineId = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(fund => fund.Id == request.PettyCashFundId)
            .SelectMany(fund => fund.Transactions)
            .Where(x => x.PettyCashRequestLineId != null && lineIds.Contains(x.PettyCashRequestLineId.Value))
            .GroupBy(x => x.PettyCashRequestLineId!.Value)
            .Select(g => new
            {
                LineId = g.Key,
                Balance = g.Sum(t => t.Direction == PettyCashTransactionDirection.In ? t.Amount : -t.Amount),
            })
            .ToDictionaryAsync(x => x.LineId, x => x.Balance, cancellationToken);

        var jobIds = request.Lines.Where(x => x.ServiceJobId != null).Select(x => x.ServiceJobId!.Value).Distinct().ToList();
        var jobNumberById = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);

        return Ok(new PettyCashRequestDto(
            request.Id,
            request.Number,
            request.PettyCashFundId,
            fundCode,
            request.RequestedByUserId,
            request.RequestedByName,
            request.RequestedAt,
            request.NeededByAt,
            request.Notes,
            request.Status,
            request.SubmittedAt,
            request.ApprovedAt,
            request.ApprovedByUserId,
            request.RejectedAt,
            request.RejectionReason,
            request.RequestedTotal,
            request.ApprovedTotal,
            request.FundedTotal,
            request.OutstandingTotal,
            request.Lines
                .OrderBy(line => line.Category)
                .ThenBy(line => line.Purpose)
                .Select(line => new PettyCashRequestLineDto(
                    line.Id,
                    line.Category,
                    line.ServiceJobId,
                    line.ServiceJobId != null ? jobNumberById.GetValueOrDefault(line.ServiceJobId.Value) : null,
                    line.CustomCategoryName,
                    line.Purpose,
                    line.RequestedAmount,
                    line.ApprovedAmount,
                    line.FundedAmount,
                    line.OutstandingAmount,
                    balanceByLineId.GetValueOrDefault(line.Id),
                    line.Fundings
                        .OrderBy(funding => funding.FundedAt)
                        .Select(funding => new PettyCashRequestLineFundingDto(
                            funding.Id,
                            funding.Amount,
                            funding.FundedAt,
                            funding.PaymentReference,
                            funding.Notes))
                        .ToList()))
                .ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PettyCashRequestDto>> UpdateHeader(
        Guid id,
        UpdatePettyCashRequestHeaderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.UpdatePettyCashRequestHeaderAsync(
            id,
            request.NeededByAt?.ToUniversalTime(),
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<PettyCashRequestDto>> AddLine(
        Guid id,
        PettyCashRequestLineRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.AddPettyCashRequestLineAsync(
            id,
            request.Category,
            request.ServiceJobId,
            request.CustomCategoryName,
            request.Purpose,
            request.RequestedAmount,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<PettyCashRequestDto>> UpdateLine(
        Guid id,
        Guid lineId,
        PettyCashRequestLineRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.UpdatePettyCashRequestLineAsync(
            id,
            lineId,
            request.Category,
            request.ServiceJobId,
            request.CustomCategoryName,
            request.Purpose,
            request.RequestedAmount,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.RemovePettyCashRequestLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestSubmit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.SubmitPettyCashRequestAsync(id, cancellationToken);
        await NotifyHeadOfficeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(
        Guid id,
        ApprovePettyCashRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestApprove, cancellationToken))
        {
            return Forbid();
        }

        var approvedAmounts = request.Lines.ToDictionary(x => x.LineId, x => x.ApprovedAmount);
        await financeService.ApprovePettyCashRequestAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            approvedAmounts,
            cancellationToken);

        await NotifyRequesterAsync(
            id,
            "Petty cash request approved",
            "Head office approved it. Money can now be released per category from the request page.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(
        Guid id,
        RejectPettyCashRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestReject, cancellationToken))
        {
            return Forbid();
        }

        await financeService.RejectPettyCashRequestAsync(id, request.Reason, cancellationToken);
        await NotifyRequesterAsync(
            id,
            "Petty cash request rejected",
            string.IsNullOrWhiteSpace(request.Reason) ? "rejected by head office." : $"rejected by head office. {request.Reason.Trim()}",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.CancelPettyCashRequestAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/lines/{lineId:guid}/fund")]
    public async Task<ActionResult> FundLine(
        Guid id,
        Guid lineId,
        FundPettyCashRequestLineRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestFund, cancellationToken))
        {
            return Forbid();
        }

        await financeService.FundPettyCashRequestLineAsync(
            id,
            lineId,
            request.Amount,
            request.FundedAt?.ToUniversalTime(),
            request.PaymentReference,
            request.Notes,
            cancellationToken);

        var reference = string.IsNullOrWhiteSpace(request.PaymentReference) ? "" : $" ({request.PaymentReference.Trim()})";
        await NotifyRequesterAsync(
            id,
            "Petty cash request released",
            $"Head office released {request.Amount:0.00}{reference} into your petty cash fund.",
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Goes to whoever can act on it - approve or release - rather than to a fixed role, so the
    /// people who actually hold the permission are the ones told there is money to decide about.
    /// </summary>
    private async Task NotifyHeadOfficeAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await dbContext.PettyCashRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.RequestedByName,
                Total = x.Lines.Sum(line => line.RequestedAmount),
                LineCount = x.Lines.Count,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return;
        }

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashRequestApprove, AppPermissions.PettyCashRequestFund],
            excludeUserId: currentUser.UserId,
            cancellationToken);

        notificationService.EnqueueInAppForUsers(
            recipients,
            "Petty cash request submitted for approval",
            $"{request.Number} from {request.RequestedByName} was submitted for head office approval. "
            + $"{request.Total:0.00} across {request.LineCount} categor{(request.LineCount == 1 ? "y" : "ies")}.",
            $"/finance/petty-cash-requests/{request.Id}",
            ReferenceTypes.PettyCashRequest,
            request.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyRequesterAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var request = await dbContext.PettyCashRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null || request.RequestedByUserId == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            request.RequestedByUserId,
            title,
            $"{request.Number}: {message}",
            $"/finance/petty-cash-requests/{request.Id}",
            ReferenceTypes.PettyCashRequest,
            request.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
}

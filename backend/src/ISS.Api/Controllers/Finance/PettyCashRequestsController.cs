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

/// <summary>
/// V2 fund replenishment workflow. Historical category-based PCRs remain readable, but every new
/// request is fund-level and is justified by a reconciliation snapshot rather than cash categories.
/// </summary>
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
    public sealed record RequestSummaryDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        DateTimeOffset? NeededByAt,
        PettyCashRequestStatus Status,
        decimal RequestedAmount,
        decimal ApprovedAmount,
        decimal FundedAmount,
        decimal OutstandingAmount,
        bool IsLegacyCategoryRequest);

    public sealed record RequestDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        decimal? AuthorizedFloat,
        string RequestedByName,
        DateTimeOffset RequestedAt,
        DateTimeOffset? NeededByAt,
        string? Notes,
        PettyCashRequestStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        decimal RequestedAmount,
        decimal ApprovedAmount,
        decimal FundedAmount,
        decimal OutstandingAmount,
        decimal CashOnHand,
        decimal OutstandingAdvances,
        decimal ReconciledExpenses,
        bool IsLegacyCategoryRequest,
        int LegacyCategoryLineCount);

    public sealed record CreateRequest(
        Guid PettyCashFundId,
        decimal RequestedAmount,
        decimal CashOnHand,
        decimal OutstandingAdvances,
        decimal ReconciledExpenses,
        DateTimeOffset? NeededByAt,
        string? Notes,
        string? RequestedByName);

    public sealed record UpdateRequest(
        decimal RequestedAmount,
        decimal CashOnHand,
        decimal OutstandingAdvances,
        decimal ReconciledExpenses,
        DateTimeOffset? NeededByAt,
        string? Notes);

    public sealed record ApproveRequest(decimal ApprovedAmount);
    public sealed record RejectRequest(string? Reason);
    public sealed record FundRequest(decimal Amount, DateTimeOffset? FundedAt, string PaymentReference, string? Notes);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RequestSummaryDto>>> List(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestView, cancellationToken)) return Forbid();

        var rows = await dbContext.PettyCashRequests.AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .Take(500)
            .Select(x => new RequestSummaryDto(
                x.Id,
                x.Number,
                x.PettyCashFundId,
                dbContext.PettyCashFunds.Where(f => f.Id == x.PettyCashFundId).Select(f => f.Code).FirstOrDefault(),
                x.RequestedByName,
                x.RequestedAt,
                x.NeededByAt,
                x.Status,
                x.IsLegacyCategoryRequest ? x.Lines.Sum(line => line.RequestedAmount) : x.RequestedAmount,
                x.IsLegacyCategoryRequest ? x.Lines.Sum(line => line.ApprovedAmount ?? 0m) : x.ApprovedAmount ?? 0m,
                x.IsLegacyCategoryRequest ? x.Lines.SelectMany(line => line.Fundings).Sum(funding => funding.Amount) : x.FundedAmount,
                x.IsLegacyCategoryRequest
                    ? x.Lines.Sum(line => Math.Max(0m, (line.ApprovedAmount ?? 0m) - line.Fundings.Sum(funding => funding.Amount)))
                    : Math.Max(0m, (x.ApprovedAmount ?? 0m) - x.FundedAmount),
                x.IsLegacyCategoryRequest))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestView, cancellationToken)) return Forbid();

        var request = await dbContext.PettyCashRequests.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Fundings)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();

        var fund = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == request.PettyCashFundId)
            .Select(x => new { x.Code, x.AuthorizedFloat })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new RequestDto(
            request.Id,
            request.Number,
            request.PettyCashFundId,
            fund?.Code,
            fund?.AuthorizedFloat,
            request.RequestedByName,
            request.RequestedAt,
            request.NeededByAt,
            request.Notes,
            request.Status,
            request.SubmittedAt,
            request.ApprovedAt,
            request.RejectedAt,
            request.RejectionReason,
            request.RequestedTotal,
            request.ApprovedTotal,
            request.FundedTotal,
            request.OutstandingTotal,
            request.CashOnHandAtRequest,
            request.OutstandingAdvancesAtRequest,
            request.ReconciledExpensesAtRequest,
            request.IsLegacyCategoryRequest,
            request.Lines.Count));
    }

    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(CreateRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestCreate, cancellationToken)) return Forbid();

        var requestedByName = string.IsNullOrWhiteSpace(request.RequestedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.RequestedByName.Trim();
        var id = await financeService.CreatePettyCashRequestAsync(
            request.PettyCashFundId,
            currentUser.UserId ?? Guid.Empty,
            requestedByName,
            request.RequestedAmount,
            request.CashOnHand,
            request.OutstandingAdvances,
            request.ReconciledExpenses,
            request.NeededByAt?.ToUniversalTime(),
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Update(Guid id, UpdateRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestEdit, cancellationToken)) return Forbid();
        await financeService.UpdatePettyCashRequestHeaderAsync(
            id,
            request.RequestedAmount,
            request.CashOnHand,
            request.OutstandingAdvances,
            request.ReconciledExpenses,
            request.NeededByAt?.ToUniversalTime(),
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestSubmit, cancellationToken)) return Forbid();
        await financeService.SubmitPettyCashRequestAsync(id, cancellationToken);
        await NotifyHeadOfficeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, ApproveRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestApprove, cancellationToken)) return Forbid();
        await financeService.ApprovePettyCashRequestAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            request.ApprovedAmount,
            cancellationToken);
        await NotifyRequesterAsync(id, "Petty cash replenishment approved", "Head office approved the replenishment request.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, RejectRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestReject, cancellationToken)) return Forbid();
        await financeService.RejectPettyCashRequestAsync(id, request.Reason, cancellationToken);
        await NotifyRequesterAsync(id, "Petty cash replenishment rejected", request.Reason ?? "Head office rejected the replenishment request.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/fund")]
    public async Task<ActionResult> Fund(Guid id, FundRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestFund, cancellationToken)) return Forbid();
        await financeService.FundPettyCashReplenishmentAsync(
            id,
            request.Amount,
            request.FundedAt?.ToUniversalTime(),
            request.PaymentReference,
            request.Notes,
            cancellationToken);
        await NotifyRequesterAsync(id, "Petty cash replenishment received", "The approved fund transfer was recorded in the petty cash ledger.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashRequestCancel, cancellationToken)) return Forbid();
        await financeService.CancelPettyCashRequestAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task NotifyHeadOfficeAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByName, x.RequestedAmount })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return;

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashRequestApprove, AppPermissions.PettyCashRequestFund],
            currentUser.UserId,
            cancellationToken);
        notificationService.EnqueueInAppForUsers(
            recipients,
            "Petty cash replenishment awaiting approval",
            $"{row.Number} from {row.RequestedByName} requests {row.RequestedAmount:0.00} after reconciliation.",
            $"/finance/petty-cash-requests/{row.Id}",
            ReferenceTypes.PettyCashRequest,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyRequesterAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var row = await dbContext.PettyCashRequests.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByUserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null || row.RequestedByUserId == Guid.Empty) return;

        notificationService.EnqueueInApp(
            row.RequestedByUserId,
            title,
            $"{row.Number}: {message}",
            $"/finance/petty-cash-requests/{row.Id}",
            ReferenceTypes.PettyCashRequest,
            row.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
}

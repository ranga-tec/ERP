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

/// <summary>V2 fund-level custody returns. Legacy category returns remain readable for audit.</summary>
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
    public sealed record CreateReturnRequest(Guid PettyCashFundId, decimal Amount, string? PreparedByName, string? Notes);
    public sealed record ReceiveReturnRequest(DateTimeOffset? ReceivedAt, string ReceiptReference);
    public sealed record RejectReturnRequest(string Reason);

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
        bool IsLegacyCategoryReturn,
        int LegacyCategoryLineCount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReturnDto>>> List(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnView, cancellationToken)) return Forbid();

        var rows = await dbContext.PettyCashReturns.AsNoTracking()
            .OrderByDescending(x => x.PreparedAt)
            .Take(500)
            .Select(x => new ReturnDto(
                x.Id,
                x.Number,
                x.PettyCashFundId,
                dbContext.PettyCashFunds.Where(f => f.Id == x.PettyCashFundId).Select(f => f.Code).FirstOrDefault(),
                x.PreparedByName,
                x.PreparedAt,
                x.Notes,
                x.Status,
                x.SubmittedAt,
                x.ReceivedAt,
                x.ReceiptReference,
                x.RejectedAt,
                x.RejectionReason,
                x.CancelledAt,
                x.IsLegacyCategoryReturn ? x.Lines.Sum(line => line.Amount) : x.FundLevelAmount,
                x.IsLegacyCategoryReturn,
                x.Lines.Count))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReturnDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnView, cancellationToken)) return Forbid();

        var row = await dbContext.PettyCashReturns.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        var fundCode = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => x.Id == row.PettyCashFundId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new ReturnDto(
            row.Id,
            row.Number,
            row.PettyCashFundId,
            fundCode,
            row.PreparedByName,
            row.PreparedAt,
            row.Notes,
            row.Status,
            row.SubmittedAt,
            row.ReceivedAt,
            row.ReceiptReference,
            row.RejectedAt,
            row.RejectionReason,
            row.CancelledAt,
            row.TotalAmount,
            row.IsLegacyCategoryReturn,
            row.Lines.Count));
    }

    [HttpPost]
    public async Task<ActionResult<ReturnDto>> Create(CreateReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnCreate, cancellationToken)) return Forbid();
        var preparedByName = string.IsNullOrWhiteSpace(request.PreparedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.PreparedByName.Trim();
        var id = await financeService.CreatePettyCashReturnAsync(
            request.PettyCashFundId,
            currentUser.UserId ?? Guid.Empty,
            preparedByName,
            request.Amount,
            request.Notes,
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
        await NotifyPreparerAsync(id, "Petty cash return received", "Head office confirmed receipt and the fund balance was reduced.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, RejectReturnRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashReturnReject, cancellationToken)) return Forbid();
        await financeService.RejectPettyCashReturnAsync(id, currentUser.UserId ?? Guid.Empty, request.Reason, cancellationToken);
        await NotifyPreparerAsync(id, "Petty cash return rejected", request.Reason.Trim(), cancellationToken);
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
            .Select(x => new { x.Id, x.Number, x.PreparedByName, x.FundLevelAmount })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return;
        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashReturnReceive, AppPermissions.PettyCashReturnReject],
            currentUser.UserId,
            cancellationToken);
        notificationService.EnqueueInAppForUsers(
            recipients,
            "Petty cash return awaiting receipt",
            $"{row.Number} from {row.PreparedByName} returns {row.FundLevelAmount:0.00}. Count the cash and record the receipt reference.",
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

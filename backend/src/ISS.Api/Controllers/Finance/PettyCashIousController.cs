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
[Route("api/finance/petty-cash-ious")]
[Authorize]
public sealed class PettyCashIousController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record PettyCashIouDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        string? ServiceJobNumber,
        Guid? ServiceJobDailySheetId,
        Guid RequestedByUserId,
        string RequestedByName,
        decimal Amount,
        string Purpose,
        DateTimeOffset RequestedAt,
        DateTimeOffset? ExpectedSettlementAt,
        PettyCashIouStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        Guid? ApprovedByUserId,
        Guid? PettyCashFundId,
        DateTimeOffset? ReleasedAt,
        string? ReleaseReference,
        DateTimeOffset? SettledAt,
        decimal? SettledAmount,
        string? SettlementReference,
        string? RejectionReason);

    public sealed record CreatePettyCashIouRequest(
        Guid ServiceJobId,
        decimal Amount,
        string Purpose,
        DateTimeOffset? ExpectedSettlementAt,
        string? RequestedByName,
        Guid? ServiceJobDailySheetId);

    public sealed record RejectPettyCashIouRequest(string? Reason);
    public sealed record ReleasePettyCashIouRequest(Guid PettyCashFundId, string? ReleaseReference);
    public sealed record SettlePettyCashIouRequest(decimal SettledAmount, string? SettlementReference);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PettyCashIouDto>>> List(
        [FromQuery] Guid? serviceJobId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var query = dbContext.PettyCashIous.AsNoTracking();
        if (serviceJobId is not null)
        {
            query = query.Where(x => x.ServiceJobId == serviceJobId.Value);
        }

        var ious = await query
            .OrderByDescending(x => x.RequestedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(ious);
    }

    [HttpPost]
    public async Task<ActionResult<PettyCashIouDto>> Create(CreatePettyCashIouRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouCreate, cancellationToken))
        {
            return Forbid();
        }

        var userId = currentUser.UserId ?? Guid.Empty;
        var requestedByName = string.IsNullOrWhiteSpace(request.RequestedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.RequestedByName;
        var expectedSettlementAtUtc = request.ExpectedSettlementAt?.ToUniversalTime();

        var id = await financeService.CreatePettyCashIouAsync(
            request.ServiceJobId,
            userId,
            requestedByName,
            request.Amount,
            request.Purpose,
            expectedSettlementAtUtc,
            request.ServiceJobDailySheetId,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PettyCashIouDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken))
        {
            return Forbid();
        }

        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

        return iou is null ? NotFound() : Ok(iou);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouSubmit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.SubmitPettyCashIouAsync(id, cancellationToken);
        await NotifyIouSubmittedAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouApprove, cancellationToken))
        {
            return Forbid();
        }

        await financeService.ApprovePettyCashIouAsync(id, currentUser.UserId ?? Guid.Empty, cancellationToken);
        await NotifyRequesterAsync(id, "IOU approved", "Your IOU request has been approved.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, RejectPettyCashIouRequest? request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouReject, cancellationToken))
        {
            return Forbid();
        }

        await financeService.RejectPettyCashIouAsync(id, request?.Reason, cancellationToken);
        await NotifyRequesterAsync(id, "IOU rejected", "Your IOU request has been rejected.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult> Release(Guid id, ReleasePettyCashIouRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouRelease, cancellationToken))
        {
            return Forbid();
        }

        await financeService.ReleasePettyCashIouAsync(id, request.PettyCashFundId, request.ReleaseReference, cancellationToken);
        await NotifyRequesterAsync(id, "IOU cash released", "Cash has been released for your IOU request.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/settle")]
    public async Task<ActionResult> Settle(Guid id, SettlePettyCashIouRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouSettle, cancellationToken))
        {
            return Forbid();
        }

        await financeService.SettlePettyCashIouAsync(id, request.SettledAmount, request.SettlementReference, cancellationToken);
        await NotifyRequesterAsync(id, "IOU settled", "Your IOU request has been settled/accounted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);

    private async Task NotifyIouSubmittedAsync(Guid id, CancellationToken cancellationToken)
    {
        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByUserId, x.RequestedByName, x.Amount, x.Purpose })
            .FirstOrDefaultAsync(cancellationToken);

        if (iou is null)
        {
            return;
        }

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashIouApprove, AppPermissions.PettyCashIouRelease],
            excludeUserId: null,
            cancellationToken);

        notificationService.EnqueueInAppForUsers(
            recipients,
            "IOU request waiting",
            $"{iou.Number} from {iou.RequestedByName} is waiting for approval/release. Amount: {iou.Amount:0.00}.",
            "/finance/petty-cash-ious",
            ReferenceTypes.PettyCashIou,
            iou.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyRequesterAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.RequestedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (iou is null || iou.RequestedByUserId == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            iou.RequestedByUserId,
            title,
            $"{iou.Number}: {message}",
            "/finance/petty-cash-ious",
            ReferenceTypes.PettyCashIou,
            iou.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PettyCashIouDto ToDto(PettyCashIou iou)
        => new(
            iou.Id,
            iou.Number,
            iou.ServiceJobId,
            null,
            iou.ServiceJobDailySheetId,
            iou.RequestedByUserId,
            iou.RequestedByName,
            iou.Amount,
            iou.Purpose,
            iou.RequestedAt,
            iou.ExpectedSettlementAt,
            iou.Status,
            iou.SubmittedAt,
            iou.ApprovedAt,
            iou.ApprovedByUserId,
            iou.PettyCashFundId,
            iou.ReleasedAt,
            iou.ReleaseReference,
            iou.SettledAt,
            iou.SettledAmount,
            iou.SettlementReference,
            iou.RejectionReason);
}

using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using ISS.Domain.Service;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    public sealed record PettyCashStaffDto(Guid UserId, string Name, string? Email);

    /// <summary>
    /// Who cash can be handed to. Lives here rather than under admin or service so Finance can
    /// reach it: the custodian issuing the cash is the one who needs the list.
    /// </summary>
    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<PettyCashStaffDto>>> Staff(CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken))
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        var users = await userManager.Users.AsNoTracking()
            .Where(x => x.LockoutEnd == null || x.LockoutEnd <= now)
            .OrderBy(x => x.DisplayName ?? x.Email)
            .Select(x => new PettyCashStaffDto(
                x.Id,
                x.DisplayName != null && x.DisplayName != "" ? x.DisplayName : (x.Email ?? x.UserName)!,
                x.Email))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    public sealed record PettyCashIouDto(
        Guid Id,
        string Number,
        Guid? ServiceJobId,
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
        string? RejectionReason,
        decimal ClaimedAmount,
        int ClaimCount,
        decimal? ReturnedAmount,
        decimal? UnaccountedAmount,
        string? IssueBillNumber,
        Guid? PettyCashRequestLineId,
        DateTimeOffset? SettlementApprovedAt);

    public sealed record CreatePettyCashIouRequest(
        Guid ServiceJobId,
        decimal Amount,
        string Purpose,
        DateTimeOffset? ExpectedSettlementAt,
        string? RequestedByName,
        Guid? ServiceJobDailySheetId);

    public sealed record RejectPettyCashIouRequest(string? Reason);
    public sealed record ReleasePettyCashIouRequest(
        Guid PettyCashFundId,
        string? ReleaseReference,
        string? IssueBillNumber,
        Guid? PettyCashRequestLineId);

    public sealed record SettlePettyCashIouRequest(decimal SettledAmount, string? SettlementReference);

    /// <summary>
    /// Cash handed over on a pre-printed slip, with no request behind it. The slip number is the
    /// document number, so it is required and must not repeat.
    /// </summary>
    public sealed record IssuePettyCashIouDirectlyRequest(
        string SlipNumber,
        decimal Amount,
        string Purpose,
        Guid PettyCashFundId,
        // The staff member the cash was handed to. The advance is theirs to settle, so this is the
        // holder of record - not whoever typed the form in.
        Guid? IssuedToUserId,
        string? IssuedToName,
        // Optional, and not on the slip: the job and funded category are attribution, recorded when
        // they are known rather than asked for at the counter.
        Guid? ServiceJobId,
        Guid? PettyCashRequestLineId);

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
            .ToListAsync(cancellationToken);

        var totals = await LoadClaimTotalsAsync(ious.Select(x => x.Id).ToList(), cancellationToken);

        return Ok(ious
            .Select(x => ToDto(x, totals.GetValueOrDefault(x.Id, IouClaimTotals.Empty)))
            .ToList());
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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (iou is null)
        {
            return NotFound();
        }

        var totals = await LoadClaimTotalsAsync(new[] { iou.Id }, cancellationToken);

        return Ok(ToDto(iou, totals.GetValueOrDefault(iou.Id, IouClaimTotals.Empty)));
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

        await financeService.ReleasePettyCashIouAsync(
            id,
            request.PettyCashFundId,
            request.ReleaseReference,
            request.IssueBillNumber,
            request.PettyCashRequestLineId,
            cancellationToken);
        await NotifyRequesterAsync(id, "IOU cash released", "Cash has been released for your IOU request.", cancellationToken);
        return NoContent();
    }

    [HttpPost("issue-directly")]
    public async Task<ActionResult<PettyCashIouDto>> IssueDirectly(
        IssuePettyCashIouDirectlyRequest request,
        CancellationToken cancellationToken)
    {
        // Issuing without a request is releasing cash, so it is gated on the release permission
        // rather than the create one.
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouRelease, cancellationToken))
        {
            return Forbid();
        }

        // The advance belongs to whoever took the cash. Falling back to the current user only
        // covers the custodian drawing it for themselves.
        var issuedToUserId = request.IssuedToUserId ?? currentUser.UserId ?? Guid.Empty;
        var issuedToName = string.IsNullOrWhiteSpace(request.IssuedToName)
            ? await ResolveUserNameAsync(issuedToUserId, cancellationToken)
            : request.IssuedToName;

        var id = await financeService.IssuePettyCashIouDirectlyAsync(
            request.ServiceJobId,
            issuedToUserId,
            issuedToName,
            request.Amount,
            request.Purpose,
            request.PettyCashFundId,
            request.SlipNumber,
            request.PettyCashRequestLineId,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/approve-settlement")]
    public async Task<ActionResult> ApproveSettlement(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouApprove, cancellationToken))
        {
            return Forbid();
        }

        await financeService.ApprovePettyCashIouSettlementAsync(id, currentUser.UserId ?? Guid.Empty, cancellationToken);
        await NotifyRequesterAsync(id, "IOU settlement approved", "Head office has approved your IOU settlement.", cancellationToken);
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

    private async Task<string> ResolveUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var name = await userManager.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.DisplayName != null && x.DisplayName != "" ? x.DisplayName : (x.Email ?? x.UserName))
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(name) ? "Unknown user" : name;
    }

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

    /// <summary>
    /// Claimed is what the technician actually documented against this advance; settled is what they
    /// declared they spent. The gap between the two is cash that left the fund with nothing to show
    /// for it, and it is the whole point of these figures — settlement itself never checks.
    /// </summary>
    private sealed record IouClaimTotals(decimal ClaimedAmount, int ClaimCount)
    {
        public static readonly IouClaimTotals Empty = new(0m, 0);
    }

    private async Task<Dictionary<Guid, IouClaimTotals>> LoadClaimTotalsAsync(
        IReadOnlyCollection<Guid> iouIds,
        CancellationToken cancellationToken)
    {
        if (iouIds.Count == 0)
        {
            return new Dictionary<Guid, IouClaimTotals>();
        }

        // Rejected claims are excluded: they document nothing, so counting them would mask a gap.
        var rows = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .Where(x => x.PettyCashIouId != null
                        && iouIds.Contains(x.PettyCashIouId.Value)
                        && x.Status != ServiceExpenseClaimStatus.Rejected)
            .GroupBy(x => x.PettyCashIouId!.Value)
            .Select(g => new
            {
                PettyCashIouId = g.Key,
                ClaimedAmount = g.Sum(claim => claim.Lines.Sum(line => line.Quantity * line.UnitCost)),
                ClaimCount = g.Count(),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.PettyCashIouId, x => new IouClaimTotals(x.ClaimedAmount, x.ClaimCount));
    }

    private static PettyCashIouDto ToDto(PettyCashIou iou, IouClaimTotals totals)
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
            iou.RejectionReason,
            totals.ClaimedAmount,
            totals.ClaimCount,
            iou.SettledAmount is null ? null : iou.Amount - iou.SettledAmount.Value,
            iou.SettledAmount is null ? null : iou.SettledAmount.Value - totals.ClaimedAmount,
            iou.IssueBillNumber,
            iou.PettyCashRequestLineId,
            iou.SettlementApprovedAt);
}

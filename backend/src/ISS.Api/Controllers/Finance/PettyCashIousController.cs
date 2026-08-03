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
        Guid? IssuedToUserId,
        string? IssuedToName,
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
        decimal? UnaccountedAmount,
        string? IssueBillNumber,
        Guid? PettyCashRequestLineId,
        DateTimeOffset? SettlementApprovedAt,
        decimal ReturnedAmount,
        decimal OutstandingAmount,
        bool IsOpenForAccounting);

    public sealed record CreatePettyCashIouRequest(
        Guid ServiceJobId,
        decimal Amount,
        string Purpose,
        DateTimeOffset? ExpectedSettlementAt,
        string? RequestedByName,
        Guid? ServiceJobDailySheetId);

    public sealed record UpdatePettyCashIouRequest(
        Guid ServiceJobId,
        decimal Amount,
        string Purpose,
        DateTimeOffset? ExpectedSettlementAt);

    public sealed record RejectPettyCashIouRequest(string? Reason);
    public sealed record ReleasePettyCashIouRequest(
        Guid PettyCashFundId,
        string? ReleaseReference,
        string IssueBillNumber,
        Guid IssuedToUserId,
        Guid? PettyCashRequestLineId);

    public sealed record SettlePettyCashIouRequest(string? SettlementReference);
    public sealed record ReturnPettyCashIouBalanceRequest(decimal Amount, string? Reference);

    public sealed record AddPettyCashIouBillRequest(
        string Description,
        decimal Amount,
        bool BillableToCustomer,
        string? ReceiptReference);

    public sealed record PettyCashIouBillDto(
        Guid Id,
        string Description,
        decimal Amount,
        bool BillableToCustomer,
        string VoucherNumber,
        ServiceExpenseClaimStatus VoucherStatus);

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
        var jobNumbers = await LoadJobNumbersAsync(ious, cancellationToken);

        return Ok(ious
            .Select(x => ToDto(
                x,
                totals.GetValueOrDefault(x.Id, IouClaimTotals.Empty),
                x.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null))
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
        var jobNumbers = await LoadJobNumbersAsync(new[] { iou }, cancellationToken);

        return Ok(ToDto(
            iou,
            totals.GetValueOrDefault(iou.Id, IouClaimTotals.Empty),
            iou.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid id,
        UpdatePettyCashIouRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouEdit, cancellationToken))
        {
            return Forbid();
        }

        await financeService.UpdatePettyCashIouBeforeApprovalAsync(
            id,
            request.ServiceJobId,
            request.Amount,
            request.Purpose,
            request.ExpectedSettlementAt?.ToUniversalTime(),
            cancellationToken);

        return NoContent();
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

        if (request.IssuedToUserId == Guid.Empty)
        {
            return BadRequest("Select the employee collecting the cash.");
        }

        var issuedToName = await userManager.Users.AsNoTracking()
            .Where(x => x.Id == request.IssuedToUserId)
            .Select(x => x.DisplayName != null && x.DisplayName != "" ? x.DisplayName : (x.Email ?? x.UserName))
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(issuedToName))
        {
            return BadRequest("The selected employee no longer exists.");
        }

        await financeService.ReleasePettyCashIouAsync(
            id,
            request.PettyCashFundId,
            request.ReleaseReference,
            request.IssueBillNumber,
            request.IssuedToUserId,
            issuedToName,
            request.PettyCashRequestLineId,
            cancellationToken);
        await NotifyIouReleasedAsync(id, cancellationToken);
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

        await financeService.SettlePettyCashIouAsync(id, request.SettlementReference, cancellationToken);
        await NotifyRequesterAsync(id, "IOU settled", "Your IOU request has been settled/accounted.", cancellationToken);
        return NoContent();
    }

    /// <summary>The bills gathered against this advance, whichever voucher they ended up on.</summary>
    [HttpGet("{id:guid}/bills")]
    public async Task<ActionResult<IReadOnlyList<PettyCashIouBillDto>>> Bills(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken))
        {
            return Forbid();
        }

        var rows = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .Where(claim => claim.PettyCashIouId == id && claim.Status != ServiceExpenseClaimStatus.Rejected)
            .SelectMany(claim => claim.Lines.Select(line => new PettyCashIouBillDto(
                line.Id,
                line.Description,
                line.Quantity * line.UnitCost,
                line.BillableToCustomer,
                claim.Number,
                claim.Status)))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("{id:guid}/bills")]
    public async Task<ActionResult> AddBill(Guid id, AddPettyCashIouBillRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouSettle, cancellationToken))
        {
            return Forbid();
        }

        await financeService.AddPettyCashIouBillAsync(
            id,
            request.Description,
            request.Amount,
            request.BillableToCustomer,
            request.ReceiptReference,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Cash handed back, in however many instalments it arrives. Credited to the category the
    /// advance was drawn from, so releasing and returning are matching entries on that sub-account.
    /// </summary>
    [HttpPost("{id:guid}/return-balance")]
    public async Task<ActionResult> ReturnBalance(
        Guid id,
        ReturnPettyCashIouBalanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouSettle, cancellationToken))
        {
            return Forbid();
        }

        await financeService.ReturnPettyCashIouBalanceAsync(id, request.Amount, request.Reference, cancellationToken);
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

    private async Task NotifyIouReleasedAsync(Guid id, CancellationToken cancellationToken)
    {
        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.RequestedByUserId,
                x.IssuedToUserId,
                x.IssuedToName,
                x.Amount,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (iou is null)
        {
            return;
        }

        var recipients = new[] { iou.RequestedByUserId, iou.IssuedToUserId ?? Guid.Empty }
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToList();

        notificationService.EnqueueInAppForUsers(
            recipients,
            "IOU cash released",
            $"{iou.Number}: {iou.Amount:0.00} was released to {iou.IssuedToName ?? "the collector"}.",
            $"/finance/petty-cash-ious/{iou.Id}",
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

    /// <summary>
    /// Job numbers resolved here rather than joined on the page: a null means the job is gone, and
    /// the screen can say so instead of printing an id at somebody.
    /// </summary>
    private async Task<Dictionary<Guid, string>> LoadJobNumbersAsync(
        IReadOnlyCollection<PettyCashIou> ious,
        CancellationToken cancellationToken)
    {
        var jobIds = ious.Where(x => x.ServiceJobId != null).Select(x => x.ServiceJobId!.Value).Distinct().ToList();
        if (jobIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);
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

    private static PettyCashIouDto ToDto(PettyCashIou iou, IouClaimTotals totals, string? serviceJobNumber)
        => new(
            iou.Id,
            iou.Number,
            iou.ServiceJobId,
            serviceJobNumber,
            iou.ServiceJobDailySheetId,
            iou.RequestedByUserId,
            iou.RequestedByName,
            iou.IssuedToUserId,
            iou.IssuedToName,
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
            // What is still outstanding after cash came back, less what the bills document. This is
            // live from the moment cash is released, not only once someone settles.
            iou.Status is PettyCashIouStatus.Draft or PettyCashIouStatus.Submitted or PettyCashIouStatus.Approved
                ? null
                : iou.OutstandingAmount - totals.ClaimedAmount,
            iou.IssueBillNumber,
            iou.PettyCashRequestLineId,
            iou.SettlementApprovedAt,
            iou.ReturnedAmount,
            iou.OutstandingAmount,
            iou.IsOpenForAccounting);
}

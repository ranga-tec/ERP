using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-iou-batches")]
[Authorize]
public sealed class PettyCashIouApprovalBatchesController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser,
    AccessControlService accessControl,
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    public sealed record CreateBatchRequest(
        Guid PettyCashFundId,
        Guid AssignedApproverUserId,
        IReadOnlyList<Guid> PettyCashIouIds);

    public sealed record ApproveBatchLineRequest(Guid LineId, decimal ApprovedAmount);
    public sealed record ApproveAssignedBatchRequest(IReadOnlyList<ApproveBatchLineRequest> Lines);
    public sealed record ReceiveFundingRequest(string FundingReference);
    public sealed record RejectBatchRequest(string? Reason);

    public sealed record BatchLineDto(
        Guid Id,
        Guid PettyCashIouId,
        string IouNumber,
        Guid? ServiceJobId,
        string? ServiceJobNumber,
        string RequestedByName,
        string Purpose,
        decimal RequestedAmount,
        decimal ApprovedAmount);

    public sealed record BatchDto(
        Guid Id,
        string Number,
        Guid PettyCashFundId,
        string? PettyCashFundCode,
        Guid ReviewerUserId,
        string ReviewerName,
        Guid AssignedApproverUserId,
        string AssignedApproverName,
        PettyCashIouApprovalBatchStatus Status,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? AssignedApprovedAt,
        DateTimeOffset? HeadOfficeSubmittedAt,
        DateTimeOffset? HeadOfficeApprovedAt,
        DateTimeOffset? FundingReceivedAt,
        string? FundingReference,
        string? RejectionReason,
        decimal RequestedTotal,
        decimal ApprovedTotal,
        bool IsReviewer,
        bool IsAssignedApprover,
        IReadOnlyList<BatchLineDto> Lines);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BatchDto>>> List(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken)) return Forbid();

        var batches = await dbContext.PettyCashIouApprovalBatches.AsNoTracking()
            .Include(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
        return Ok(await ToDtosAsync(batches, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouView, cancellationToken)) return Forbid();

        var batch = await dbContext.PettyCashIouApprovalBatches.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (batch is null) return NotFound();
        return Ok((await ToDtosAsync([batch], cancellationToken)).Single());
    }

    [HttpPost]
    public async Task<ActionResult<BatchDto>> Create(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouReview, cancellationToken)) return Forbid();
        var reviewerUserId = currentUser.UserId ?? Guid.Empty;
        var reviewerName = await ResolveUserNameAsync(reviewerUserId, cancellationToken);
        var approverName = await ResolveActiveUserNameAsync(request.AssignedApproverUserId, cancellationToken);
        if (approverName is null) return BadRequest("The selected approver does not exist or is locked.");

        var id = await financeService.CreatePettyCashIouApprovalBatchAsync(
            request.PettyCashFundId,
            reviewerUserId,
            reviewerName,
            request.AssignedApproverUserId,
            approverName,
            request.PettyCashIouIds,
            cancellationToken);
        await NotifyUserAsync(
            request.AssignedApproverUserId,
            id,
            "Petty cash batch assigned",
            "Review the IOU breakdown, edit approved amounts where needed, and approve the batch.",
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPost("{id:guid}/approve-assigned")]
    public async Task<ActionResult> ApproveAssigned(
        Guid id,
        ApproveAssignedBatchRequest request,
        CancellationToken cancellationToken)
    {
        var amounts = request.Lines.ToDictionary(x => x.LineId, x => x.ApprovedAmount);
        await financeService.ApproveAssignedPettyCashIouBatchAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            amounts,
            cancellationToken);
        await NotifyReviewerAsync(
            id,
            "Petty cash batch returned by approver",
            "The assigned approver completed the batch review. Submit the complete batch to head office.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit-head-office")]
    public async Task<ActionResult> SubmitHeadOffice(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouReview, cancellationToken)) return Forbid();
        await financeService.SubmitPettyCashIouBatchToHeadOfficeAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            cancellationToken);

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.PettyCashIouApprove, AppPermissions.PettyCashIouReject],
            excludeUserId: currentUser.UserId,
            cancellationToken);
        await NotifyUsersAsync(
            recipients,
            id,
            "Petty cash batch awaiting head-office approval",
            "Review the fund, total and IOU/job breakdown, then approve or reject the batch.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouApprove, cancellationToken)) return Forbid();
        await financeService.ApprovePettyCashIouBatchAtHeadOfficeAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            cancellationToken);
        await NotifyReviewerAsync(
            id,
            "Petty cash batch approved by head office",
            "Record the remittance reference when the approved money is received into the selected fund.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive-funding")]
    public async Task<ActionResult> ReceiveFunding(
        Guid id,
        ReceiveFundingRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouRelease, cancellationToken)) return Forbid();
        await financeService.ReceivePettyCashIouBatchFundingAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            request.FundingReference,
            cancellationToken);
        await NotifyBatchRequestersAsync(
            id,
            "Petty cash funding received",
            "The approved advance funding is now in the selected petty cash fund and can be released against signed IOU slips.",
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id, RejectBatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.PettyCashIouReject, cancellationToken)) return Forbid();
        await financeService.RejectPettyCashIouBatchAsync(
            id,
            currentUser.UserId ?? Guid.Empty,
            request.Reason,
            cancellationToken);
        await NotifyBatchRequestersAsync(
            id,
            "Petty cash batch rejected",
            string.IsNullOrWhiteSpace(request.Reason) ? "The approval batch was rejected." : request.Reason.Trim(),
            cancellationToken);
        return NoContent();
    }

    private async Task<IReadOnlyList<BatchDto>> ToDtosAsync(
        IReadOnlyCollection<PettyCashIouApprovalBatch> batches,
        CancellationToken cancellationToken)
    {
        var iouIds = batches.SelectMany(x => x.Lines).Select(x => x.PettyCashIouId).Distinct().ToList();
        var ious = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => iouIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var jobIds = ious.Values.Where(x => x.ServiceJobId != null).Select(x => x.ServiceJobId!.Value).Distinct().ToList();
        var jobNumbers = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Number, cancellationToken);
        var fundIds = batches.Select(x => x.PettyCashFundId).Distinct().ToList();
        var fundCodes = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(x => fundIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var userId = currentUser.UserId;

        return batches.Select(batch => new BatchDto(
            batch.Id,
            batch.Number,
            batch.PettyCashFundId,
            fundCodes.GetValueOrDefault(batch.PettyCashFundId),
            batch.ReviewerUserId,
            batch.ReviewerName,
            batch.AssignedApproverUserId,
            batch.AssignedApproverName,
            batch.Status,
            batch.SubmittedAt,
            batch.AssignedApprovedAt,
            batch.HeadOfficeSubmittedAt,
            batch.HeadOfficeApprovedAt,
            batch.FundingReceivedAt,
            batch.FundingReference,
            batch.RejectionReason,
            batch.RequestedTotal,
            batch.ApprovedTotal,
            userId == batch.ReviewerUserId,
            userId == batch.AssignedApproverUserId,
            batch.Lines.Select(line =>
            {
                var iou = ious[line.PettyCashIouId];
                return new BatchLineDto(
                    line.Id,
                    iou.Id,
                    iou.Number,
                    iou.ServiceJobId,
                    iou.ServiceJobId is { } jobId ? jobNumbers.GetValueOrDefault(jobId) : null,
                    iou.RequestedByName,
                    iou.Purpose,
                    line.RequestedAmount,
                    line.ApprovedAmount);
            }).ToList())).ToList();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        => currentUser.UserId is { } userId
           && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);

    private async Task<string> ResolveUserNameAsync(Guid userId, CancellationToken cancellationToken)
        => await userManager.Users.AsNoTracking()
               .Where(x => x.Id == userId)
               .Select(x => x.DisplayName != null && x.DisplayName != "" ? x.DisplayName : (x.Email ?? x.UserName))
               .FirstOrDefaultAsync(cancellationToken)
           ?? "Unknown user";

    private async Task<string?> ResolveActiveUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await userManager.Users.AsNoTracking()
            .Where(x => x.Id == userId && (x.LockoutEnd == null || x.LockoutEnd <= now))
            .Select(x => x.DisplayName != null && x.DisplayName != "" ? x.DisplayName : (x.Email ?? x.UserName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task NotifyReviewerAsync(
        Guid batchId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var reviewer = await dbContext.PettyCashIouApprovalBatches.AsNoTracking()
            .Where(x => x.Id == batchId)
            .Select(x => x.ReviewerUserId)
            .FirstAsync(cancellationToken);
        await NotifyUserAsync(reviewer, batchId, title, message, cancellationToken);
    }

    private async Task NotifyBatchRequestersAsync(
        Guid batchId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var recipients = await (
                from line in dbContext.PettyCashIouApprovalBatchLines.AsNoTracking()
                join iou in dbContext.PettyCashIous.AsNoTracking() on line.PettyCashIouId equals iou.Id
                where line.PettyCashIouApprovalBatchId == batchId
                select iou.RequestedByUserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        await NotifyUsersAsync(recipients, batchId, title, message, cancellationToken);
    }

    private Task NotifyUserAsync(
        Guid userId,
        Guid batchId,
        string title,
        string message,
        CancellationToken cancellationToken)
        => NotifyUsersAsync([userId], batchId, title, message, cancellationToken);

    private async Task NotifyUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid batchId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var number = await dbContext.PettyCashIouApprovalBatches.AsNoTracking()
            .Where(x => x.Id == batchId)
            .Select(x => x.Number)
            .FirstAsync(cancellationToken);
        notificationService.EnqueueInAppForUsers(
            userIds.Where(x => x != Guid.Empty).Distinct().ToList(),
            title,
            $"{number}: {message}",
            "/finance/petty-cash-ious",
            ReferenceTypes.PettyCashIouApprovalBatch,
            batchId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

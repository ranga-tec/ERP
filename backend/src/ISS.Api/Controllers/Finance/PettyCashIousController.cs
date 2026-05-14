using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/petty-cash-ious")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance},{Roles.Service}")]
public sealed class PettyCashIousController(
    IIssDbContext dbContext,
    FinanceService financeService,
    ICurrentUser currentUser) : ControllerBase
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
        var userId = currentUser.UserId ?? Guid.Empty;
        var requestedByName = string.IsNullOrWhiteSpace(request.RequestedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.RequestedByName;

        var id = await financeService.CreatePettyCashIouAsync(
            request.ServiceJobId,
            userId,
            requestedByName,
            request.Amount,
            request.Purpose,
            request.ExpectedSettlementAt,
            request.ServiceJobDailySheetId,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PettyCashIouDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var iou = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

        return iou is null ? NotFound() : Ok(iou);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await financeService.SubmitPettyCashIouAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await financeService.ApprovePettyCashIouAsync(id, currentUser.UserId ?? Guid.Empty, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Reject(Guid id, RejectPettyCashIouRequest? request, CancellationToken cancellationToken)
    {
        await financeService.RejectPettyCashIouAsync(id, request?.Reason, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Release(Guid id, ReleasePettyCashIouRequest request, CancellationToken cancellationToken)
    {
        await financeService.ReleasePettyCashIouAsync(id, request.PettyCashFundId, request.ReleaseReference, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/settle")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
    public async Task<ActionResult> Settle(Guid id, SettlePettyCashIouRequest request, CancellationToken cancellationToken)
    {
        await financeService.SettlePettyCashIouAsync(id, request.SettledAmount, request.SettlementReference, cancellationToken);
        return NoContent();
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

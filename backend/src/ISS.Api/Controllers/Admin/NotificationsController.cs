using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = $"{Roles.Admin}")]
public sealed class NotificationsController(IIssDbContext dbContext, IClock clock) : ControllerBase
{
    public sealed record NotificationDto(
        Guid Id,
        NotificationChannel Channel,
        string Recipient,
        string? Subject,
        string Body,
        NotificationStatus Status,
        int Attempts,
        DateTimeOffset NextAttemptAt,
        DateTimeOffset? LastAttemptAt,
        DateTimeOffset? SentAt,
        string? LastError,
        string? ReferenceType,
        Guid? ReferenceId,
        DateTimeOffset CreatedAt);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var query = dbContext.NotificationOutboxItems.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new NotificationDto(
                x.Id,
                x.Channel,
                x.Recipient,
                x.Subject,
                x.Body,
                x.Status,
                x.Attempts,
                x.NextAttemptAt,
                x.LastAttemptAt,
                x.SentAt,
                x.LastError,
                x.ReferenceType,
                x.ReferenceId,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.NotificationOutboxItems.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new NotificationDto(
                x.Id,
                x.Channel,
                x.Recipient,
                x.Subject,
                x.Body,
                x.Status,
                x.Attempts,
                x.NextAttemptAt,
                x.LastAttemptAt,
                x.SentAt,
                x.LastError,
                x.ReferenceType,
                x.ReferenceId,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.NotificationOutboxItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.RetryNow(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}


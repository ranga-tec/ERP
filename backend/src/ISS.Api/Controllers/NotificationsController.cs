using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(
    IIssDbContext dbContext,
    ICurrentUser currentUser,
    IClock clock) : ControllerBase
{
    public sealed record UserNotificationDto(
        Guid Id,
        string Title,
        string Message,
        string? Href,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt,
        string? ReferenceType,
        Guid? ReferenceId);

    public sealed record NotificationCountDto(int UnreadCount);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserNotificationDto>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        take = Math.Clamp(take, 1, 200);
        var query = dbContext.UserNotifications.AsNoTracking()
            .Where(x => x.RecipientUserId == userId);

        if (unreadOnly)
        {
            query = query.Where(x => x.ReadAt == null);
        }

        var notifications = await query
            .OrderByDescending(x => x.NotificationCreatedAt)
            .Take(take)
            .Select(x => new UserNotificationDto(
                x.Id,
                x.Title,
                x.Message,
                x.Href,
                x.NotificationCreatedAt,
                x.ReadAt,
                x.ReferenceType,
                x.ReferenceId))
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationCountDto>> UnreadCount(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var count = await dbContext.UserNotifications.AsNoTracking()
            .CountAsync(x => x.RecipientUserId == userId && x.ReadAt == null, cancellationToken);
        return Ok(new NotificationCountDto(count));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var notification = await dbContext.UserNotifications
            .FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        notification.MarkRead(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var unread = await dbContext.UserNotifications
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

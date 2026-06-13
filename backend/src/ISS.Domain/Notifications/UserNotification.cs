using ISS.Domain.Common;

namespace ISS.Domain.Notifications;

public sealed class UserNotification : AuditableEntity
{
    private UserNotification() { }

    public UserNotification(
        Guid recipientUserId,
        string title,
        string message,
        string? href,
        DateTimeOffset createdAt,
        string? referenceType,
        Guid? referenceId)
    {
        RecipientUserId = recipientUserId;
        Title = Guard.NotNullOrWhiteSpace(title, nameof(title), 160);
        Message = Guard.NotNullOrWhiteSpace(message, nameof(message), 1000);
        Href = string.IsNullOrWhiteSpace(href) ? null : Guard.NotNullOrWhiteSpace(href, nameof(href), 512);
        NotificationCreatedAt = createdAt;
        ReferenceType = string.IsNullOrWhiteSpace(referenceType)
            ? null
            : Guard.NotNullOrWhiteSpace(referenceType, nameof(referenceType), 64);
        ReferenceId = referenceId;
    }

    public Guid RecipientUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? Href { get; private set; }
    public DateTimeOffset NotificationCreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public bool IsRead => ReadAt is not null;

    public void MarkRead(DateTimeOffset readAt)
    {
        ReadAt ??= readAt;
    }
}

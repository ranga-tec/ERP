using ISS.Domain.Common;

namespace ISS.Domain.Notifications;

public enum NotificationChannel
{
    Email = 1,
    Sms = 2
}

public enum NotificationStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}

public sealed class NotificationOutboxItem : AuditableEntity
{
    private NotificationOutboxItem() { }

    public NotificationOutboxItem(
        NotificationChannel channel,
        string recipient,
        string? subject,
        string body,
        DateTimeOffset queuedAt,
        string? referenceType,
        Guid? referenceId)
    {
        Channel = channel;
        Recipient = Guard.NotNullOrWhiteSpace(recipient, nameof(Recipient), maxLength: 256);
        Subject = subject?.Trim();
        Body = Guard.NotNullOrWhiteSpace(body, nameof(Body), maxLength: 8000);
        Status = NotificationStatus.Pending;
        Attempts = 0;
        NextAttemptAt = queuedAt;
        ReferenceType = string.IsNullOrWhiteSpace(referenceType)
            ? null
            : Guard.NotNullOrWhiteSpace(referenceType, nameof(referenceType), maxLength: 64);
        ReferenceId = referenceId;
    }

    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = null!;

    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? LastError { get; private set; }

    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public void MarkProcessing(DateTimeOffset now)
    {
        if (Status is NotificationStatus.Sent or NotificationStatus.Failed)
        {
            throw new DomainValidationException("Cannot process a completed notification.");
        }

        Status = NotificationStatus.Processing;
        Attempts++;
        LastAttemptAt = now;
    }

    public void MarkSent(DateTimeOffset now)
    {
        Status = NotificationStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    public void MarkFailed(DateTimeOffset now, string error, TimeSpan retryDelay, int maxAttempts)
    {
        LastError = error.Length > 2000 ? error[..2000] : error;

        if (Attempts >= maxAttempts)
        {
            Status = NotificationStatus.Failed;
            NextAttemptAt = DateTimeOffset.MaxValue;
            return;
        }

        Status = NotificationStatus.Pending;
        NextAttemptAt = now.Add(retryDelay);
    }

    public void RetryNow(DateTimeOffset now)
    {
        if (Status == NotificationStatus.Sent)
        {
            throw new DomainValidationException("Sent notifications cannot be retried.");
        }

        Status = NotificationStatus.Pending;
        NextAttemptAt = now;
    }
}


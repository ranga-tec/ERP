using ISS.Domain.Common;

namespace ISS.Domain.Audit;

public enum AuditAction
{
    Insert = 1,
    Update = 2,
    Delete = 3
}

public sealed class AuditLog : Entity
{
    private AuditLog() { }

    public AuditLog(
        DateTimeOffset occurredAt,
        Guid? userId,
        string tableName,
        AuditAction action,
        string key,
        string changesJson)
    {
        OccurredAt = occurredAt;
        UserId = userId;
        TableName = Guard.NotNullOrWhiteSpace(tableName, nameof(tableName), maxLength: 256);
        Action = action;
        Key = Guard.NotNullOrWhiteSpace(key, nameof(key), maxLength: 256);
        ChangesJson = Guard.NotNullOrWhiteSpace(changesJson, nameof(changesJson), maxLength: 100_000);
    }

    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? UserId { get; private set; }
    public string TableName { get; private set; } = null!;
    public AuditAction Action { get; private set; }
    public string Key { get; private set; } = null!;
    public string ChangesJson { get; private set; } = null!;
}


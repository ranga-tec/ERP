using ISS.Domain.Common;

namespace ISS.Domain.Service;

public sealed class QualityCheck : AuditableEntity
{
    private QualityCheck() { }

    public QualityCheck(Guid serviceJobId, DateTimeOffset checkedAt, bool passed, string? notes)
    {
        ServiceJobId = serviceJobId;
        CheckedAt = checkedAt;
        Passed = passed;
        Notes = notes?.Trim();
    }

    public Guid ServiceJobId { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }
    public bool Passed { get; private set; }
    public string? Notes { get; private set; }
}


namespace ISS.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }
    public Guid? LastModifiedBy { get; set; }
}


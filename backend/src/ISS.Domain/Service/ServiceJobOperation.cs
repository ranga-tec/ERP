using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobOperationStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3
}

public sealed class ServiceJobOperation : AuditableEntity
{
    private ServiceJobOperation() { }

    public ServiceJobOperation(
        Guid serviceJobId,
        int sequence,
        string name,
        string? description,
        Guid? plannedItemId,
        decimal plannedQuantity,
        decimal estimatedLaborHours,
        DateTimeOffset? requiredAt,
        string? notes)
    {
        ServiceJobId = serviceJobId;
        Status = ServiceJobOperationStatus.Planned;
        Update(sequence, name, description, plannedItemId, plannedQuantity, estimatedLaborHours, requiredAt, notes);
    }

    public Guid ServiceJobId { get; private set; }
    public int Sequence { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? PlannedItemId { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal EstimatedLaborHours { get; private set; }
    public DateTimeOffset? RequiredAt { get; private set; }
    public string? Notes { get; private set; }
    public ServiceJobOperationStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void Update(
        int sequence,
        string name,
        string? description,
        Guid? plannedItemId,
        decimal plannedQuantity,
        decimal estimatedLaborHours,
        DateTimeOffset? requiredAt,
        string? notes)
    {
        if (Status == ServiceJobOperationStatus.Completed)
        {
            throw new DomainValidationException("Completed job operations cannot be edited.");
        }

        if (sequence <= 0)
        {
            throw new DomainValidationException("Operation sequence must be positive.");
        }

        if (plannedItemId is null && plannedQuantity != 0m)
        {
            throw new DomainValidationException("Planned quantity requires a planned item.");
        }

        Sequence = sequence;
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), 256);
        Description = NormalizeOptional(description, nameof(description), 2000);
        PlannedItemId = plannedItemId;
        PlannedQuantity = Guard.NotNegative(plannedQuantity, nameof(plannedQuantity));
        EstimatedLaborHours = Guard.NotNegative(estimatedLaborHours, nameof(estimatedLaborHours));
        RequiredAt = requiredAt;
        Notes = NormalizeOptional(notes, nameof(notes), 2000);
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status is not ServiceJobOperationStatus.Planned)
        {
            throw new DomainValidationException("Only planned job operations can be started.");
        }

        Status = ServiceJobOperationStatus.InProgress;
        StartedAt = startedAt;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is not (ServiceJobOperationStatus.Planned or ServiceJobOperationStatus.InProgress))
        {
            throw new DomainValidationException("Only planned or in-progress job operations can be completed.");
        }

        Status = ServiceJobOperationStatus.Completed;
        StartedAt ??= completedAt;
        CompletedAt = completedAt;
    }

    public void Skip()
    {
        if (Status == ServiceJobOperationStatus.Completed)
        {
            throw new DomainValidationException("Completed job operations cannot be skipped.");
        }

        Status = ServiceJobOperationStatus.Skipped;
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Guard.NotNullOrWhiteSpace(value, paramName, maxLength);
}

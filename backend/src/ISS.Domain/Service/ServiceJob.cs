using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Closed = 3,
    Cancelled = 4
}

public sealed class ServiceJob : AuditableEntity
{
    private ServiceJob() { }

    public ServiceJob(string number, Guid equipmentUnitId, Guid customerId, DateTimeOffset openedAt, string problemDescription)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        EquipmentUnitId = equipmentUnitId;
        CustomerId = customerId;
        OpenedAt = openedAt;
        ProblemDescription = Guard.NotNullOrWhiteSpace(problemDescription, nameof(ProblemDescription), maxLength: 2000);
        Status = ServiceJobStatus.Open;
    }

    public string Number { get; private set; } = null!;
    public Guid EquipmentUnitId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public string ProblemDescription { get; private set; } = null!;
    public ServiceJobStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void Start()
    {
        if (Status != ServiceJobStatus.Open)
        {
            throw new DomainValidationException("Only open service jobs can be started.");
        }

        Status = ServiceJobStatus.InProgress;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is not (ServiceJobStatus.Open or ServiceJobStatus.InProgress))
        {
            throw new DomainValidationException("Service job must be open or in progress to complete.");
        }

        Status = ServiceJobStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Close()
    {
        if (Status != ServiceJobStatus.Completed)
        {
            throw new DomainValidationException("Only completed service jobs can be closed.");
        }

        Status = ServiceJobStatus.Closed;
    }

    public void Cancel()
    {
        if (Status == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot be cancelled.");
        }

        Status = ServiceJobStatus.Cancelled;
    }
}


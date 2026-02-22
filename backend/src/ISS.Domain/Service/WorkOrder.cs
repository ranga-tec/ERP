using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum WorkOrderStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}

public sealed class WorkOrder : AuditableEntity
{
    private WorkOrder() { }

    public WorkOrder(Guid serviceJobId, string description, Guid? assignedToUserId)
    {
        ServiceJobId = serviceJobId;
        Description = Guard.NotNullOrWhiteSpace(description, nameof(Description), maxLength: 2000);
        AssignedToUserId = assignedToUserId;
        Status = WorkOrderStatus.Open;
    }

    public Guid ServiceJobId { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid? AssignedToUserId { get; private set; }
    public WorkOrderStatus Status { get; private set; }

    public void Assign(Guid? userId) => AssignedToUserId = userId;

    public void Start()
    {
        if (Status != WorkOrderStatus.Open)
        {
            throw new DomainValidationException("Only open work orders can be started.");
        }

        Status = WorkOrderStatus.InProgress;
    }

    public void MarkDone()
    {
        if (Status != WorkOrderStatus.InProgress)
        {
            throw new DomainValidationException("Work order must be in progress to mark done.");
        }

        Status = WorkOrderStatus.Done;
    }

    public void Cancel()
    {
        if (Status == WorkOrderStatus.Done)
        {
            throw new DomainValidationException("Done work orders cannot be cancelled.");
        }

        Status = WorkOrderStatus.Cancelled;
    }
}


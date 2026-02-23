using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceHandoverStatus
{
    Draft = 0,
    Completed = 1,
    Cancelled = 2
}

public sealed class ServiceHandover : AuditableEntity
{
    private ServiceHandover() { }

    public ServiceHandover(
        string number,
        Guid serviceJobId,
        DateTimeOffset handoverDate,
        string itemsReturned,
        int? postServiceWarrantyMonths,
        string? customerAcknowledgement,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        ServiceJobId = serviceJobId;
        HandoverDate = handoverDate;
        ItemsReturned = Guard.NotNullOrWhiteSpace(itemsReturned, nameof(ItemsReturned), maxLength: 2000);
        if (postServiceWarrantyMonths is < 0)
        {
            throw new DomainValidationException("Post-service warranty months cannot be negative.");
        }

        PostServiceWarrantyMonths = postServiceWarrantyMonths;
        CustomerAcknowledgement = customerAcknowledgement?.Trim();
        Notes = notes?.Trim();
        Status = ServiceHandoverStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public DateTimeOffset HandoverDate { get; private set; }
    public string ItemsReturned { get; private set; } = null!;
    public int? PostServiceWarrantyMonths { get; private set; }
    public string? CustomerAcknowledgement { get; private set; }
    public string? Notes { get; private set; }
    public ServiceHandoverStatus Status { get; private set; }

    public void Complete()
    {
        if (Status != ServiceHandoverStatus.Draft)
        {
            throw new DomainValidationException("Only draft handovers can be completed.");
        }

        Status = ServiceHandoverStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == ServiceHandoverStatus.Completed)
        {
            throw new DomainValidationException("Completed handovers cannot be cancelled.");
        }

        Status = ServiceHandoverStatus.Cancelled;
    }
}

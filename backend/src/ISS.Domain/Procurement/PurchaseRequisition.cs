using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum PurchaseRequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public sealed class PurchaseRequisition : AuditableEntity
{
    private PurchaseRequisition() { }

    public PurchaseRequisition(string number, DateTimeOffset requestDate, string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        RequestDate = requestDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.NotNullOrWhiteSpace(notes, nameof(Notes), maxLength: 2000);
        Status = PurchaseRequisitionStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public DateTimeOffset RequestDate { get; private set; }
    public PurchaseRequisitionStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public List<PurchaseRequisitionLine> Lines { get; private set; } = new();

    public PurchaseRequisitionLine AddLine(Guid itemId, decimal quantity, string? notes)
    {
        EnsureDraftEditable();

        var line = new PurchaseRequisitionLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), notes);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, string? notes)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Purchase requisition line not found.");

        line.Update(quantity, notes);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Purchase requisition line not found.");

        Lines.Remove(line);
    }

    private void EnsureDraftEditable()
    {
        if (Status != PurchaseRequisitionStatus.Draft)
        {
            throw new DomainValidationException("Only draft purchase requisitions can be edited.");
        }
    }

    public void Submit()
    {
        if (Status != PurchaseRequisitionStatus.Draft)
        {
            throw new DomainValidationException("Only draft purchase requisitions can be submitted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Purchase requisition must have at least one line.");
        }

        Status = PurchaseRequisitionStatus.Submitted;
    }

    public void Approve()
    {
        if (Status != PurchaseRequisitionStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted purchase requisitions can be approved.");
        }

        Status = PurchaseRequisitionStatus.Approved;
    }

    public void Reject()
    {
        if (Status != PurchaseRequisitionStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted purchase requisitions can be rejected.");
        }

        Status = PurchaseRequisitionStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status is PurchaseRequisitionStatus.Approved or PurchaseRequisitionStatus.Rejected)
        {
            throw new DomainValidationException("Approved or rejected purchase requisitions cannot be cancelled.");
        }

        Status = PurchaseRequisitionStatus.Cancelled;
    }
}

public sealed class PurchaseRequisitionLine : Entity
{
    private PurchaseRequisitionLine() { }

    public PurchaseRequisitionLine(Guid purchaseRequisitionId, Guid itemId, decimal quantity, string? notes)
    {
        PurchaseRequisitionId = purchaseRequisitionId;
        ItemId = itemId;
        Quantity = quantity;
        Notes = notes?.Trim();
    }

    public Guid PurchaseRequisitionId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    public void Update(decimal quantity, string? notes)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        Notes = notes?.Trim();
    }
}

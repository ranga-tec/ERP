using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Approved = 1,
    PartiallyReceived = 2,
    Closed = 3,
    Cancelled = 4
}

public sealed class PurchaseOrder : AuditableEntity
{
    private PurchaseOrder() { }

    public PurchaseOrder(string number, Guid supplierId, DateTimeOffset orderDate)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SupplierId = supplierId;
        OrderDate = orderDate;
        Status = PurchaseOrderStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }

    public List<PurchaseOrderLine> Lines { get; private set; } = new();

    public PurchaseOrderLine AddLine(Guid itemId, decimal quantity, decimal unitPrice)
    {
        EnsureDraftEditable();

        var line = new PurchaseOrderLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitPrice, nameof(unitPrice)));
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, decimal unitPrice)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
                   ?? throw new DomainValidationException("PO line not found.");

        line.Update(quantity, unitPrice);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
                   ?? throw new DomainValidationException("PO line not found.");

        Lines.Remove(line);
    }

    public decimal Total => Lines.Sum(l => l.LineTotal);

    private void EnsureDraftEditable()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new DomainValidationException("Only draft POs can be edited.");
        }
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new DomainValidationException("Only draft POs can be approved.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("PO must have at least one line.");
        }

        Status = PurchaseOrderStatus.Approved;
    }

    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Closed)
        {
            throw new DomainValidationException("Closed POs cannot be cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
    }

    public void ApplyReceipt(Guid itemId, decimal quantityReceived)
    {
        var remaining = quantityReceived;
        foreach (var line in Lines.Where(l => l.ItemId == itemId))
        {
            if (remaining <= 0)
            {
                break;
            }

            remaining = line.ApplyReceipt(remaining);
        }

        var fullyReceived = Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        if (fullyReceived)
        {
            Status = PurchaseOrderStatus.Closed;
        }
        else
        {
            Status = PurchaseOrderStatus.PartiallyReceived;
        }
    }
}

public sealed class PurchaseOrderLine : Entity
{
    private PurchaseOrderLine() { }

    public PurchaseOrderLine(Guid purchaseOrderId, Guid itemId, decimal orderedQuantity, decimal unitPrice)
    {
        PurchaseOrderId = purchaseOrderId;
        ItemId = itemId;
        OrderedQuantity = orderedQuantity;
        UnitPrice = unitPrice;
        ReceivedQuantity = 0m;
    }

    public Guid PurchaseOrderId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => OrderedQuantity * UnitPrice;

    public void Update(decimal orderedQuantity, decimal unitPrice)
    {
        var nextQuantity = Guard.Positive(orderedQuantity, nameof(orderedQuantity));
        if (nextQuantity < ReceivedQuantity)
        {
            throw new DomainValidationException("Ordered quantity cannot be less than received quantity.");
        }

        OrderedQuantity = nextQuantity;
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
    }

    public decimal ApplyReceipt(decimal quantity)
    {
        var remainingForLine = OrderedQuantity - ReceivedQuantity;
        if (remainingForLine <= 0)
        {
            return quantity;
        }

        var applied = Math.Min(remainingForLine, quantity);
        ReceivedQuantity += applied;
        return quantity - applied;
    }
}

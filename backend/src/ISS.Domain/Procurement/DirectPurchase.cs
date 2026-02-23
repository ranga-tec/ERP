using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum DirectPurchaseStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class DirectPurchase : AuditableEntity
{
    private DirectPurchase() { }

    public DirectPurchase(string number, Guid supplierId, Guid warehouseId, DateTimeOffset purchasedAt, string? remarks)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SupplierId = supplierId;
        WarehouseId = warehouseId;
        PurchasedAt = purchasedAt;
        Remarks = remarks?.Trim();
        Status = DirectPurchaseStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset PurchasedAt { get; private set; }
    public string? Remarks { get; private set; }
    public DirectPurchaseStatus Status { get; private set; }

    public List<DirectPurchaseLine> Lines { get; private set; } = new();

    public DirectPurchaseLine AddLine(Guid itemId, decimal quantity, decimal unitPrice, decimal taxPercent, string? batchNumber)
    {
        if (Status != DirectPurchaseStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct purchases can be edited.");
        }

        var line = new DirectPurchaseLine(
            Id,
            itemId,
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitPrice, nameof(unitPrice)),
            Guard.NotNegative(taxPercent, nameof(taxPercent)),
            batchNumber);

        Lines.Add(line);
        return line;
    }

    public decimal Subtotal => Lines.Sum(l => l.Quantity * l.UnitPrice);
    public decimal TaxTotal => Lines.Sum(l => l.Quantity * l.UnitPrice * (l.TaxPercent / 100m));
    public decimal GrandTotal => Subtotal + TaxTotal;

    public void Post()
    {
        if (Status != DirectPurchaseStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct purchases can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Direct purchase must have at least one line.");
        }

        Status = DirectPurchaseStatus.Posted;
    }

    public void Void()
    {
        if (Status == DirectPurchaseStatus.Voided)
        {
            return;
        }

        if (Status != DirectPurchaseStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct purchases can be voided.");
        }

        Status = DirectPurchaseStatus.Voided;
    }
}

public sealed class DirectPurchaseLine : Entity
{
    private DirectPurchaseLine() { }

    public DirectPurchaseLine(Guid directPurchaseId, Guid itemId, decimal quantity, decimal unitPrice, decimal taxPercent, string? batchNumber)
    {
        DirectPurchaseId = directPurchaseId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxPercent = taxPercent;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid DirectPurchaseId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxPercent { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<DirectPurchaseLineSerial> Serials { get; private set; } = new();

    public decimal LineSubTotal => Quantity * UnitPrice;
    public decimal LineTax => LineSubTotal * (TaxPercent / 100m);
    public decimal LineTotal => LineSubTotal + LineTax;

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new DirectPurchaseLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }
}

public sealed class DirectPurchaseLineSerial : Entity
{
    private DirectPurchaseLineSerial() { }

    public DirectPurchaseLineSerial(Guid directPurchaseLineId, string serialNumber)
    {
        DirectPurchaseLineId = directPurchaseLineId;
        SerialNumber = serialNumber;
    }

    public Guid DirectPurchaseLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

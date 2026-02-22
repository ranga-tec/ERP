using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum GoodsReceiptStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class GoodsReceipt : AuditableEntity
{
    private GoodsReceipt() { }

    public GoodsReceipt(string number, Guid purchaseOrderId, Guid warehouseId, DateTimeOffset receivedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        PurchaseOrderId = purchaseOrderId;
        WarehouseId = warehouseId;
        ReceivedAt = receivedAt;
        Status = GoodsReceiptStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid PurchaseOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public GoodsReceiptStatus Status { get; private set; }

    public List<GoodsReceiptLine> Lines { get; private set; } = new();

    public GoodsReceiptLine AddLine(Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        var line = new GoodsReceiptLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitCost, nameof(unitCost)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != GoodsReceiptStatus.Draft)
        {
            throw new DomainValidationException("Only draft GRNs can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("GRN must have at least one line.");
        }

        Status = GoodsReceiptStatus.Posted;
    }

    public void Void()
    {
        if (Status == GoodsReceiptStatus.Voided)
        {
            return;
        }

        if (Status != GoodsReceiptStatus.Draft)
        {
            throw new DomainValidationException("Only draft GRNs can be voided.");
        }

        Status = GoodsReceiptStatus.Voided;
    }
}

public sealed class GoodsReceiptLine : Entity
{
    private GoodsReceiptLine() { }

    public GoodsReceiptLine(Guid goodsReceiptId, Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        GoodsReceiptId = goodsReceiptId;
        ItemId = itemId;
        Quantity = quantity;
        UnitCost = unitCost;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid GoodsReceiptId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<GoodsReceiptLineSerial> Serials { get; private set; } = new();

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new GoodsReceiptLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }
}

public sealed class GoodsReceiptLineSerial : Entity
{
    private GoodsReceiptLineSerial() { }

    public GoodsReceiptLineSerial(Guid goodsReceiptLineId, string serialNumber)
    {
        GoodsReceiptLineId = goodsReceiptLineId;
        SerialNumber = serialNumber;
    }

    public Guid GoodsReceiptLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}


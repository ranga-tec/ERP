using ISS.Domain.Common;

namespace ISS.Domain.Inventory;

public enum StockAdjustmentStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class StockAdjustment : AuditableEntity
{
    private StockAdjustment() { }

    public StockAdjustment(string number, Guid warehouseId, DateTimeOffset adjustedAt, string? reason)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        WarehouseId = warehouseId;
        AdjustedAt = adjustedAt;
        Reason = reason?.Trim();
        Status = StockAdjustmentStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset AdjustedAt { get; private set; }
    public string? Reason { get; private set; }
    public StockAdjustmentStatus Status { get; private set; }

    public List<StockAdjustmentLine> Lines { get; private set; } = new();

    public StockAdjustmentLine AddLine(Guid itemId, decimal quantityDelta, decimal unitCost, string? batchNumber)
    {
        if (Status != StockAdjustmentStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock adjustments can be edited.");
        }

        if (quantityDelta == 0m)
        {
            throw new DomainValidationException("Quantity delta cannot be zero.");
        }

        var line = new StockAdjustmentLine(Id, itemId, quantityDelta, Guard.NotNegative(unitCost, nameof(unitCost)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != StockAdjustmentStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock adjustments can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Stock adjustment must have at least one line.");
        }

        Status = StockAdjustmentStatus.Posted;
    }

    public void Void()
    {
        if (Status == StockAdjustmentStatus.Voided)
        {
            return;
        }

        if (Status != StockAdjustmentStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock adjustments can be voided.");
        }

        Status = StockAdjustmentStatus.Voided;
    }
}

public sealed class StockAdjustmentLine : Entity
{
    private StockAdjustmentLine() { }

    public StockAdjustmentLine(Guid stockAdjustmentId, Guid itemId, decimal quantityDelta, decimal unitCost, string? batchNumber)
    {
        StockAdjustmentId = stockAdjustmentId;
        ItemId = itemId;
        QuantityDelta = quantityDelta;
        UnitCost = unitCost;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid StockAdjustmentId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<StockAdjustmentLineSerial> Serials { get; private set; } = new();

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new StockAdjustmentLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }
}

public sealed class StockAdjustmentLineSerial : Entity
{
    private StockAdjustmentLineSerial() { }

    public StockAdjustmentLineSerial(Guid stockAdjustmentLineId, string serialNumber)
    {
        StockAdjustmentLineId = stockAdjustmentLineId;
        SerialNumber = serialNumber;
    }

    public Guid StockAdjustmentLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}


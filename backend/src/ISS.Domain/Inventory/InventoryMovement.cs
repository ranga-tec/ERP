using ISS.Domain.Common;

namespace ISS.Domain.Inventory;

public enum InventoryMovementType
{
    Receipt = 1,
    Issue = 2,
    Adjustment = 3,
    TransferIn = 4,
    TransferOut = 5,
    Consumption = 6,
    SupplierReturn = 7
}

public sealed class InventoryMovement : AuditableEntity
{
    private InventoryMovement() { }

    public InventoryMovement(
        DateTimeOffset occurredAt,
        InventoryMovementType type,
        Guid warehouseId,
        Guid itemId,
        decimal quantity,
        decimal unitCost,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        string? serialNumber,
        string? batchNumber)
    {
        OccurredAt = occurredAt;
        Type = type;
        WarehouseId = warehouseId;
        ItemId = itemId;
        Quantity = quantity;
        UnitCost = Guard.NotNegative(unitCost, nameof(UnitCost));
        ReferenceType = Guard.NotNullOrWhiteSpace(referenceType, nameof(ReferenceType), maxLength: 64);
        ReferenceId = referenceId;
        ReferenceLineId = referenceLineId;
        SerialNumber = serialNumber?.Trim();
        BatchNumber = batchNumber?.Trim();
    }

    public DateTimeOffset OccurredAt { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ItemId { get; private set; }

    /// <summary>
    /// Positive = stock in; Negative = stock out.
    /// </summary>
    public decimal Quantity { get; private set; }

    public decimal UnitCost { get; private set; }
    public string ReferenceType { get; private set; } = null!;
    public Guid ReferenceId { get; private set; }
    public Guid? ReferenceLineId { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? BatchNumber { get; private set; }
}


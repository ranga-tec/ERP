using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class ReorderSetting : AuditableEntity
{
    private ReorderSetting() { }

    public ReorderSetting(Guid warehouseId, Guid itemId, decimal reorderPoint, decimal reorderQuantity)
    {
        WarehouseId = warehouseId;
        ItemId = itemId;
        ReorderPoint = Guard.NotNegative(reorderPoint, nameof(ReorderPoint));
        ReorderQuantity = Guard.Positive(reorderQuantity, nameof(ReorderQuantity));
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    public Guid ItemId { get; private set; }
    public Item Item { get; private set; } = null!;

    public decimal ReorderPoint { get; private set; }
    public decimal ReorderQuantity { get; private set; }

    public void Update(decimal reorderPoint, decimal reorderQuantity)
    {
        ReorderPoint = Guard.NotNegative(reorderPoint, nameof(ReorderPoint));
        ReorderQuantity = Guard.Positive(reorderQuantity, nameof(ReorderQuantity));
    }
}


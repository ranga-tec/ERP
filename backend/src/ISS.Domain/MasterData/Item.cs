using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public enum ItemType
{
    Equipment = 1,
    SparePart = 2,
    Service = 3
}

public enum TrackingType
{
    None = 0,
    Serial = 1,
    Batch = 2
}

public sealed class Item : AuditableEntity
{
    private Item() { }

    public Item(
        string sku,
        string name,
        ItemType type,
        TrackingType trackingType,
        string unitOfMeasure,
        Guid? brandId,
        string? barcode,
        decimal defaultUnitCost,
        Guid? categoryId = null,
        Guid? subcategoryId = null)
    {
        Sku = Guard.NotNullOrWhiteSpace(sku, nameof(Sku), maxLength: 64);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Type = type;
        TrackingType = trackingType;
        UnitOfMeasure = Guard.NotNullOrWhiteSpace(unitOfMeasure, nameof(UnitOfMeasure), maxLength: 32);
        BrandId = brandId;
        Barcode = barcode?.Trim();
        DefaultUnitCost = Guard.NotNegative(defaultUnitCost, nameof(DefaultUnitCost));
        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        IsActive = true;
    }

    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ItemType Type { get; private set; }
    public TrackingType TrackingType { get; private set; }
    public string UnitOfMeasure { get; private set; } = null!;
    public Guid? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public Guid? CategoryId { get; private set; }
    public ItemCategory? Category { get; private set; }
    public Guid? SubcategoryId { get; private set; }
    public ItemSubcategory? Subcategory { get; private set; }
    public string? Barcode { get; private set; }
    public decimal DefaultUnitCost { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string sku,
        string name,
        ItemType type,
        TrackingType trackingType,
        string unitOfMeasure,
        Guid? brandId,
        string? barcode,
        decimal defaultUnitCost,
        bool isActive)
        => Update(
            sku,
            name,
            type,
            trackingType,
            unitOfMeasure,
            brandId,
            barcode,
            defaultUnitCost,
            isActive,
            CategoryId,
            SubcategoryId);

    public void Update(
        string sku,
        string name,
        ItemType type,
        TrackingType trackingType,
        string unitOfMeasure,
        Guid? brandId,
        string? barcode,
        decimal defaultUnitCost,
        bool isActive,
        Guid? categoryId,
        Guid? subcategoryId)
    {
        Sku = Guard.NotNullOrWhiteSpace(sku, nameof(Sku), maxLength: 64);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 256);
        Type = type;
        TrackingType = trackingType;
        UnitOfMeasure = Guard.NotNullOrWhiteSpace(unitOfMeasure, nameof(UnitOfMeasure), maxLength: 32);
        BrandId = brandId;
        Barcode = barcode?.Trim();
        DefaultUnitCost = Guard.NotNegative(defaultUnitCost, nameof(DefaultUnitCost));
        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        IsActive = isActive;
    }
}

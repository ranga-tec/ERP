using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class WarehouseBin : AuditableEntity
{
    private WarehouseBin() { }

    public WarehouseBin(Guid warehouseId, string code, string name, string? zone, string? rack, string? shelf)
    {
        WarehouseId = warehouseId == Guid.Empty ? throw new DomainValidationException("Warehouse is required.") : warehouseId;
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Zone = NormalizeOptional(zone);
        Rack = NormalizeOptional(rack);
        Shelf = NormalizeOptional(shelf);
        IsActive = true;
    }

    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Zone { get; private set; }
    public string? Rack { get; private set; }
    public string? Shelf { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string? zone, string? rack, string? shelf, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Zone = NormalizeOptional(zone);
        Rack = NormalizeOptional(rack);
        Shelf = NormalizeOptional(shelf);
        IsActive = isActive;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

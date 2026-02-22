using ISS.Domain.Common;

namespace ISS.Domain.Service;

public sealed class EquipmentUnit : AuditableEntity
{
    private EquipmentUnit() { }

    public EquipmentUnit(Guid itemId, string serialNumber, Guid customerId, DateTimeOffset? purchasedAt, DateTimeOffset? warrantyUntil)
    {
        ItemId = itemId;
        SerialNumber = Guard.NotNullOrWhiteSpace(serialNumber, nameof(SerialNumber), maxLength: 128);
        CustomerId = customerId;
        PurchasedAt = purchasedAt;
        WarrantyUntil = warrantyUntil;
    }

    public Guid ItemId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset? PurchasedAt { get; private set; }
    public DateTimeOffset? WarrantyUntil { get; private set; }

    public void Update(Guid customerId, DateTimeOffset? purchasedAt, DateTimeOffset? warrantyUntil)
    {
        CustomerId = customerId;
        PurchasedAt = purchasedAt;
        WarrantyUntil = warrantyUntil;
    }
}


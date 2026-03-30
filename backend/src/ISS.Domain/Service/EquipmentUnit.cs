using ISS.Domain.Common;

namespace ISS.Domain.Service;

public sealed class EquipmentUnit : AuditableEntity
{
    private EquipmentUnit() { }

    public EquipmentUnit(
        Guid itemId,
        string serialNumber,
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        ServiceCoverageScope warrantyCoverage)
    {
        ItemId = itemId;
        SerialNumber = Guard.NotNullOrWhiteSpace(serialNumber, nameof(SerialNumber), maxLength: 128);
        Update(customerId, purchasedAt, warrantyUntil, warrantyCoverage);
    }

    public Guid ItemId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset? PurchasedAt { get; private set; }
    public DateTimeOffset? WarrantyUntil { get; private set; }
    public ServiceCoverageScope WarrantyCoverage { get; private set; }

    public void Update(
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        ServiceCoverageScope warrantyCoverage)
    {
        if (warrantyUntil is null && warrantyCoverage != ServiceCoverageScope.None)
        {
            throw new DomainValidationException("Warranty coverage requires a warranty end date.");
        }

        if (warrantyUntil is not null && warrantyCoverage == ServiceCoverageScope.None)
        {
            throw new DomainValidationException("Select warranty coverage when a warranty end date is provided.");
        }

        CustomerId = customerId;
        PurchasedAt = purchasedAt;
        WarrantyUntil = warrantyUntil;
        WarrantyCoverage = warrantyUntil is null ? ServiceCoverageScope.None : warrantyCoverage;
    }

    public bool HasActiveWarranty(DateTimeOffset when)
        => WarrantyUntil is not null
           && WarrantyCoverage != ServiceCoverageScope.None
           && WarrantyUntil.Value >= when;
}

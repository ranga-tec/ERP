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
        ServiceCoverageScope warrantyCoverage,
        int? serviceIntervalDays = null,
        DateTimeOffset? nextServiceDueAt = null,
        DateTimeOffset? nextRepairDueAt = null)
    {
        ItemId = itemId;
        SerialNumber = Guard.NotNullOrWhiteSpace(serialNumber, nameof(SerialNumber), maxLength: 128);
        Update(customerId, purchasedAt, warrantyUntil, warrantyCoverage, serviceIntervalDays, nextServiceDueAt, nextRepairDueAt);
    }

    public Guid ItemId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset? PurchasedAt { get; private set; }
    public DateTimeOffset? WarrantyUntil { get; private set; }
    public ServiceCoverageScope WarrantyCoverage { get; private set; }
    public int? ServiceIntervalDays { get; private set; }
    public DateTimeOffset? NextServiceDueAt { get; private set; }
    public DateTimeOffset? NextRepairDueAt { get; private set; }

    public void Update(
        Guid customerId,
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        ServiceCoverageScope warrantyCoverage,
        int? serviceIntervalDays = null,
        DateTimeOffset? nextServiceDueAt = null,
        DateTimeOffset? nextRepairDueAt = null)
    {
        if (warrantyUntil is null && warrantyCoverage != ServiceCoverageScope.None)
        {
            throw new DomainValidationException("Warranty coverage requires a warranty end date.");
        }

        if (warrantyUntil is not null && warrantyCoverage == ServiceCoverageScope.None)
        {
            throw new DomainValidationException("Select warranty coverage when a warranty end date is provided.");
        }

        if (serviceIntervalDays is <= 0)
        {
            throw new DomainValidationException("Service interval days must be positive.");
        }

        CustomerId = customerId;
        PurchasedAt = purchasedAt;
        WarrantyUntil = warrantyUntil;
        WarrantyCoverage = warrantyUntil is null ? ServiceCoverageScope.None : warrantyCoverage;
        ServiceIntervalDays = serviceIntervalDays;
        NextServiceDueAt = nextServiceDueAt ?? CalculateNextServiceDueAt(purchasedAt, warrantyUntil, serviceIntervalDays);
        NextRepairDueAt = nextRepairDueAt;
    }

    public bool HasActiveWarranty(DateTimeOffset when)
        => WarrantyUntil is not null
           && WarrantyCoverage != ServiceCoverageScope.None
           && WarrantyUntil.Value >= when;

    private static DateTimeOffset? CalculateNextServiceDueAt(
        DateTimeOffset? purchasedAt,
        DateTimeOffset? warrantyUntil,
        int? serviceIntervalDays)
    {
        if (serviceIntervalDays is null)
        {
            return warrantyUntil;
        }

        if (purchasedAt is null)
        {
            return warrantyUntil;
        }

        var dueAt = purchasedAt.Value.AddDays(serviceIntervalDays.Value);
        return warrantyUntil is not null && dueAt > warrantyUntil ? warrantyUntil : dueAt;
    }
}

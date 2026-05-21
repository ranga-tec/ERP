using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobMaterialDispositionKind
{
    Used = 0,
    UnusedReturned = 1,
    IncorrectReturned = 2,
    Damaged = 3,
    RejectedSupplierReturn = 4
}

public enum ServiceJobMaterialChargeTo
{
    Customer = 0,
    Company = 1,
    Supplier = 2,
    Employee = 3,
    Warranty = 4
}

public enum ServiceJobMaterialDispositionStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class ServiceJobMaterialDisposition : AuditableEntity
{
    private ServiceJobMaterialDisposition() { }

    public ServiceJobMaterialDisposition(
        Guid serviceJobId,
        Guid materialRequisitionId,
        Guid materialRequisitionLineId,
        Guid itemId,
        Guid warehouseId,
        ServiceJobMaterialDispositionKind kind,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        string condition,
        string reason,
        ServiceJobMaterialChargeTo chargeTo,
        Guid? supplierReturnId,
        string? responsiblePerson,
        Guid? serviceJobDailySheetId = null)
    {
        ServiceJobId = serviceJobId;
        ServiceJobDailySheetId = serviceJobDailySheetId;
        MaterialRequisitionId = materialRequisitionId;
        MaterialRequisitionLineId = materialRequisitionLineId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        Kind = kind;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
        BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : Guard.NotNullOrWhiteSpace(batchNumber, nameof(batchNumber), 128);
        Condition = Guard.NotNullOrWhiteSpace(condition, nameof(condition), 128);
        Reason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), 1000);
        ChargeTo = chargeTo;
        SupplierReturnId = supplierReturnId;
        ResponsiblePerson = string.IsNullOrWhiteSpace(responsiblePerson) ? null : Guard.NotNullOrWhiteSpace(responsiblePerson, nameof(responsiblePerson), 256);
    }

    public Guid ServiceJobId { get; private set; }
    public Guid? ServiceJobDailySheetId { get; private set; }
    public Guid MaterialRequisitionId { get; private set; }
    public Guid MaterialRequisitionLineId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public ServiceJobMaterialDispositionKind Kind { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? BatchNumber { get; private set; }
    public string Condition { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public ServiceJobMaterialChargeTo ChargeTo { get; private set; }
    public Guid? SupplierReturnId { get; private set; }
    public string? ResponsiblePerson { get; private set; }
    public bool IsVoided { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public string? VoidReason { get; private set; }
    public List<ServiceJobMaterialDispositionSerial> Serials { get; private set; } = new();
    public decimal CostImpact => Quantity * UnitCost;
    public bool IsPosted => PostedAt is not null && !IsVoided;
    public ServiceJobMaterialDispositionStatus Status => IsVoided
        ? ServiceJobMaterialDispositionStatus.Voided
        : PostedAt is null
            ? ServiceJobMaterialDispositionStatus.Draft
            : ServiceJobMaterialDispositionStatus.Posted;

    public void UpdateDetails(
        string condition,
        string reason,
        ServiceJobMaterialChargeTo chargeTo,
        Guid? supplierReturnId,
        string? responsiblePerson)
    {
        if (IsVoided)
        {
            throw new DomainValidationException("Voided material disposition cannot be edited.");
        }

        if (IsPosted)
        {
            throw new DomainValidationException("Posted material disposition cannot be edited.");
        }

        Condition = Guard.NotNullOrWhiteSpace(condition, nameof(condition), 128);
        Reason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), 1000);
        ChargeTo = chargeTo;
        SupplierReturnId = supplierReturnId;
        ResponsiblePerson = string.IsNullOrWhiteSpace(responsiblePerson) ? null : Guard.NotNullOrWhiteSpace(responsiblePerson, nameof(responsiblePerson), 256);
    }

    public void Void(DateTimeOffset voidedAt, string? reason)
    {
        if (IsVoided)
        {
            throw new DomainValidationException("Material disposition is already voided.");
        }

        IsVoided = true;
        VoidedAt = voidedAt;
        VoidReason = string.IsNullOrWhiteSpace(reason) ? null : Guard.NotNullOrWhiteSpace(reason, nameof(reason), 512);
    }

    public void Post(DateTimeOffset postedAt)
    {
        if (IsVoided)
        {
            throw new DomainValidationException("Voided material disposition cannot be posted.");
        }

        if (IsPosted)
        {
            throw new DomainValidationException("Material disposition is already posted.");
        }

        PostedAt = postedAt;
    }

    public void ReplaceSerials(IReadOnlyCollection<string>? serialNumbers)
    {
        if (IsVoided)
        {
            throw new DomainValidationException("Voided material disposition cannot be edited.");
        }

        if (IsPosted)
        {
            throw new DomainValidationException("Posted material disposition cannot be edited.");
        }

        var normalizedSerials = serialNumbers?
            .Select(serial => Guard.NotNullOrWhiteSpace(serial, nameof(serial), 128))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        Serials.Clear();
        foreach (var serial in normalizedSerials)
        {
            Serials.Add(new ServiceJobMaterialDispositionSerial(Id, serial));
        }
    }
}

public sealed class ServiceJobMaterialDispositionSerial : Entity
{
    private ServiceJobMaterialDispositionSerial() { }

    public ServiceJobMaterialDispositionSerial(Guid serviceJobMaterialDispositionId, string serialNumber)
    {
        ServiceJobMaterialDispositionId = serviceJobMaterialDispositionId;
        SerialNumber = serialNumber;
    }

    public Guid ServiceJobMaterialDispositionId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

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
        string? responsiblePerson)
    {
        ServiceJobId = serviceJobId;
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
    public List<ServiceJobMaterialDispositionSerial> Serials { get; private set; } = new();
    public decimal CostImpact => Quantity * UnitCost;

    public void ReplaceSerials(IReadOnlyCollection<string>? serialNumbers)
    {
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

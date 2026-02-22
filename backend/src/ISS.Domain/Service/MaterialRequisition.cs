using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum MaterialRequisitionStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class MaterialRequisition : AuditableEntity
{
    private MaterialRequisition() { }

    public MaterialRequisition(string number, Guid serviceJobId, Guid warehouseId, DateTimeOffset requestedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        ServiceJobId = serviceJobId;
        WarehouseId = warehouseId;
        RequestedAt = requestedAt;
        Status = MaterialRequisitionStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public MaterialRequisitionStatus Status { get; private set; }

    public List<MaterialRequisitionLine> Lines { get; private set; } = new();

    public MaterialRequisitionLine AddLine(Guid itemId, decimal quantity, string? batchNumber)
    {
        var line = new MaterialRequisitionLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != MaterialRequisitionStatus.Draft)
        {
            throw new DomainValidationException("Only draft material requisitions can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Material requisition must have at least one line.");
        }

        Status = MaterialRequisitionStatus.Posted;
    }

    public void Void()
    {
        if (Status == MaterialRequisitionStatus.Voided)
        {
            return;
        }

        if (Status != MaterialRequisitionStatus.Draft)
        {
            throw new DomainValidationException("Only draft material requisitions can be voided.");
        }

        Status = MaterialRequisitionStatus.Voided;
    }
}

public sealed class MaterialRequisitionLine : Entity
{
    private MaterialRequisitionLine() { }

    public MaterialRequisitionLine(Guid materialRequisitionId, Guid itemId, decimal quantity, string? batchNumber)
    {
        MaterialRequisitionId = materialRequisitionId;
        ItemId = itemId;
        Quantity = quantity;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid MaterialRequisitionId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<MaterialRequisitionLineSerial> Serials { get; private set; } = new();

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new MaterialRequisitionLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }
}

public sealed class MaterialRequisitionLineSerial : Entity
{
    private MaterialRequisitionLineSerial() { }

    public MaterialRequisitionLineSerial(Guid materialRequisitionLineId, string serialNumber)
    {
        MaterialRequisitionLineId = materialRequisitionLineId;
        SerialNumber = serialNumber;
    }

    public Guid MaterialRequisitionLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}


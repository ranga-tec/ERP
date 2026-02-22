using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum SupplierReturnStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class SupplierReturn : AuditableEntity
{
    private SupplierReturn() { }

    public SupplierReturn(string number, Guid supplierId, Guid warehouseId, DateTimeOffset returnDate, string? reason)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SupplierId = supplierId;
        WarehouseId = warehouseId;
        ReturnDate = returnDate;
        Reason = reason?.Trim();
        Status = SupplierReturnStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset ReturnDate { get; private set; }
    public string? Reason { get; private set; }
    public SupplierReturnStatus Status { get; private set; }

    public List<SupplierReturnLine> Lines { get; private set; } = new();

    public SupplierReturnLine AddLine(Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        var line = new SupplierReturnLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitCost, nameof(unitCost)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void Post()
    {
        if (Status != SupplierReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft supplier returns can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Supplier return must have at least one line.");
        }

        Status = SupplierReturnStatus.Posted;
    }

    public void Void()
    {
        if (Status == SupplierReturnStatus.Voided)
        {
            return;
        }

        if (Status != SupplierReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft supplier returns can be voided.");
        }

        Status = SupplierReturnStatus.Voided;
    }
}

public sealed class SupplierReturnLine : Entity
{
    private SupplierReturnLine() { }

    public SupplierReturnLine(Guid supplierReturnId, Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        SupplierReturnId = supplierReturnId;
        ItemId = itemId;
        Quantity = quantity;
        UnitCost = unitCost;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid SupplierReturnId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<SupplierReturnLineSerial> Serials { get; private set; } = new();

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new SupplierReturnLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }
}

public sealed class SupplierReturnLineSerial : Entity
{
    private SupplierReturnLineSerial() { }

    public SupplierReturnLineSerial(Guid supplierReturnLineId, string serialNumber)
    {
        SupplierReturnLineId = supplierReturnLineId;
        SerialNumber = serialNumber;
    }

    public Guid SupplierReturnLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}


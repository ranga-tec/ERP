using ISS.Domain.Common;

namespace ISS.Domain.Inventory;

public enum StockTransferStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class StockTransfer : AuditableEntity
{
    private StockTransfer() { }

    public StockTransfer(string number, Guid fromWarehouseId, Guid toWarehouseId, DateTimeOffset transferDate, string? notes)
    {
        if (fromWarehouseId == toWarehouseId)
        {
            throw new DomainValidationException("From and To warehouses must be different.");
        }

        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        FromWarehouseId = fromWarehouseId;
        ToWarehouseId = toWarehouseId;
        TransferDate = transferDate;
        Notes = notes?.Trim();
        Status = StockTransferStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid FromWarehouseId { get; private set; }
    public Guid ToWarehouseId { get; private set; }
    public DateTimeOffset TransferDate { get; private set; }
    public string? Notes { get; private set; }
    public StockTransferStatus Status { get; private set; }

    public List<StockTransferLine> Lines { get; private set; } = new();

    public StockTransferLine AddLine(Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        EnsureDraftEditable();

        var line = new StockTransferLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitCost, nameof(unitCost)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        decimal quantity,
        decimal unitCost,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Stock transfer line not found.");

        line.Update(quantity, unitCost, batchNumber);
        line.ReplaceSerials(serialNumbers);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Stock transfer line not found.");

        Lines.Remove(line);
    }

    public void Post()
    {
        if (Status != StockTransferStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock transfers can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Stock transfer must have at least one line.");
        }

        Status = StockTransferStatus.Posted;
    }

    public void Void()
    {
        if (Status == StockTransferStatus.Voided)
        {
            return;
        }

        if (Status != StockTransferStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock transfers can be voided.");
        }

        Status = StockTransferStatus.Voided;
    }

    private void EnsureDraftEditable()
    {
        if (Status != StockTransferStatus.Draft)
        {
            throw new DomainValidationException("Only draft stock transfers can be edited.");
        }
    }
}

public sealed class StockTransferLine : Entity
{
    private StockTransferLine() { }

    public StockTransferLine(Guid stockTransferId, Guid itemId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        StockTransferId = stockTransferId;
        ItemId = itemId;
        Quantity = quantity;
        UnitCost = unitCost;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid StockTransferId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<StockTransferLineSerial> Serials { get; private set; } = new();

    public void Update(decimal quantity, decimal unitCost, string? batchNumber)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
        BatchNumber = batchNumber?.Trim();
    }

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new StockTransferLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
    }

    public void ReplaceSerials(IReadOnlyCollection<string>? serialNumbers)
    {
        Serials.Clear();
        if (serialNumbers is null)
        {
            return;
        }

        foreach (var serial in serialNumbers)
        {
            AddSerial(serial);
        }
    }
}

public sealed class StockTransferLineSerial : Entity
{
    private StockTransferLineSerial() { }

    public StockTransferLineSerial(Guid stockTransferLineId, string serialNumber)
    {
        StockTransferLineId = stockTransferLineId;
        SerialNumber = serialNumber;
    }

    public Guid StockTransferLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

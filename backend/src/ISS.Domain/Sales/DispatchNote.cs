using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum DispatchStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class DispatchNote : AuditableEntity
{
    private DispatchNote() { }

    public DispatchNote(string number, Guid salesOrderId, Guid warehouseId, DateTimeOffset dispatchedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SalesOrderId = salesOrderId;
        WarehouseId = warehouseId;
        DispatchedAt = dispatchedAt;
        Status = DispatchStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SalesOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset DispatchedAt { get; private set; }
    public DispatchStatus Status { get; private set; }

    public List<DispatchLine> Lines { get; private set; } = new();

    public DispatchLine AddLine(Guid itemId, decimal quantity, string? batchNumber)
    {
        EnsureDraftEditable();

        var line = new DispatchLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, string? batchNumber, IReadOnlyCollection<string>? serialNumbers)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Dispatch line not found.");

        line.Update(quantity, batchNumber);
        line.ReplaceSerials(serialNumbers);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Dispatch line not found.");

        Lines.Remove(line);
    }

    public void Post()
    {
        if (Status != DispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft dispatch notes can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Dispatch note must have at least one line.");
        }

        Status = DispatchStatus.Posted;
    }

    public void Void()
    {
        if (Status == DispatchStatus.Voided)
        {
            return;
        }

        if (Status != DispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft dispatch notes can be voided.");
        }

        Status = DispatchStatus.Voided;
    }

    private void EnsureDraftEditable()
    {
        if (Status != DispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft dispatch notes can be edited.");
        }
    }
}

public sealed class DispatchLine : Entity
{
    private DispatchLine() { }

    public DispatchLine(Guid dispatchNoteId, Guid itemId, decimal quantity, string? batchNumber)
    {
        DispatchNoteId = dispatchNoteId;
        ItemId = itemId;
        Quantity = quantity;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid DispatchNoteId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<DispatchLineSerial> Serials { get; private set; } = new();

    public void Update(decimal quantity, string? batchNumber)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        BatchNumber = batchNumber?.Trim();
    }

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new DispatchLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
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

public sealed class DispatchLineSerial : Entity
{
    private DispatchLineSerial() { }

    public DispatchLineSerial(Guid dispatchLineId, string serialNumber)
    {
        DispatchLineId = dispatchLineId;
        SerialNumber = serialNumber;
    }

    public Guid DispatchLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

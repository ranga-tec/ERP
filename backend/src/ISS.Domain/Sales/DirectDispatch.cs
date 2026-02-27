using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum DirectDispatchStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class DirectDispatch : AuditableEntity
{
    private DirectDispatch() { }

    public DirectDispatch(
        string number,
        Guid warehouseId,
        DateTimeOffset dispatchedAt,
        Guid? customerId,
        Guid? serviceJobId,
        string? reason)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        WarehouseId = warehouseId;
        DispatchedAt = dispatchedAt;
        CustomerId = customerId;
        ServiceJobId = serviceJobId;
        Reason = reason?.Trim();

        if (CustomerId is null && ServiceJobId is null)
        {
            throw new DomainValidationException("Direct dispatch requires a customer or service job reference.");
        }

        Status = DirectDispatchStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset DispatchedAt { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? ServiceJobId { get; private set; }
    public string? Reason { get; private set; }
    public DirectDispatchStatus Status { get; private set; }

    public List<DirectDispatchLine> Lines { get; private set; } = new();

    public DirectDispatchLine AddLine(Guid itemId, decimal quantity, string? batchNumber)
    {
        EnsureDraftEditable();

        var line = new DirectDispatchLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), batchNumber);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, string? batchNumber, IReadOnlyCollection<string>? serialNumbers)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Direct dispatch line not found.");

        line.Update(quantity, batchNumber);
        line.ReplaceSerials(serialNumbers);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Direct dispatch line not found.");

        Lines.Remove(line);
    }

    public void Post()
    {
        if (Status != DirectDispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct dispatches can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Direct dispatch must have at least one line.");
        }

        Status = DirectDispatchStatus.Posted;
    }

    public void Void()
    {
        if (Status == DirectDispatchStatus.Voided)
        {
            return;
        }

        if (Status != DirectDispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct dispatches can be voided.");
        }

        Status = DirectDispatchStatus.Voided;
    }

    private void EnsureDraftEditable()
    {
        if (Status != DirectDispatchStatus.Draft)
        {
            throw new DomainValidationException("Only draft direct dispatches can be edited.");
        }
    }
}

public sealed class DirectDispatchLine : Entity
{
    private DirectDispatchLine() { }

    public DirectDispatchLine(Guid directDispatchId, Guid itemId, decimal quantity, string? batchNumber)
    {
        DirectDispatchId = directDispatchId;
        ItemId = itemId;
        Quantity = quantity;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid DirectDispatchId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<DirectDispatchLineSerial> Serials { get; private set; } = new();

    public void Update(decimal quantity, string? batchNumber)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        BatchNumber = batchNumber?.Trim();
    }

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new DirectDispatchLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
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

public sealed class DirectDispatchLineSerial : Entity
{
    private DirectDispatchLineSerial() { }

    public DirectDispatchLineSerial(Guid directDispatchLineId, string serialNumber)
    {
        DirectDispatchLineId = directDispatchLineId;
        SerialNumber = serialNumber;
    }

    public Guid DirectDispatchLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

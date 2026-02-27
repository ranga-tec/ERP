using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum CustomerReturnStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public sealed class CustomerReturn : AuditableEntity
{
    private CustomerReturn() { }

    public CustomerReturn(
        string number,
        Guid customerId,
        Guid warehouseId,
        DateTimeOffset returnDate,
        Guid? salesInvoiceId,
        Guid? dispatchNoteId,
        string? reason)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        CustomerId = customerId;
        WarehouseId = warehouseId;
        ReturnDate = returnDate;
        SalesInvoiceId = salesInvoiceId;
        DispatchNoteId = dispatchNoteId;
        Reason = reason?.Trim();
        Status = CustomerReturnStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset ReturnDate { get; private set; }
    public Guid? SalesInvoiceId { get; private set; }
    public Guid? DispatchNoteId { get; private set; }
    public string? Reason { get; private set; }
    public CustomerReturnStatus Status { get; private set; }

    public List<CustomerReturnLine> Lines { get; private set; } = new();

    public CustomerReturnLine AddLine(Guid itemId, decimal quantity, decimal unitPrice, string? batchNumber)
    {
        EnsureDraftEditable();

        var line = new CustomerReturnLine(
            Id,
            itemId,
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitPrice, nameof(unitPrice)),
            batchNumber);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        string? batchNumber,
        IReadOnlyCollection<string>? serialNumbers)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Customer return line not found.");

        line.Update(quantity, unitPrice, batchNumber);
        line.ReplaceSerials(serialNumbers);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Customer return line not found.");

        Lines.Remove(line);
    }

    public void Post()
    {
        if (Status != CustomerReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft customer returns can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Customer return must have at least one line.");
        }

        Status = CustomerReturnStatus.Posted;
    }

    public void Void()
    {
        if (Status == CustomerReturnStatus.Voided)
        {
            return;
        }

        if (Status != CustomerReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft customer returns can be voided.");
        }

        Status = CustomerReturnStatus.Voided;
    }

    private void EnsureDraftEditable()
    {
        if (Status != CustomerReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft customer returns can be edited.");
        }
    }
}

public sealed class CustomerReturnLine : Entity
{
    private CustomerReturnLine() { }

    public CustomerReturnLine(Guid customerReturnId, Guid itemId, decimal quantity, decimal unitPrice, string? batchNumber)
    {
        CustomerReturnId = customerReturnId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        BatchNumber = batchNumber?.Trim();
    }

    public Guid CustomerReturnId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? BatchNumber { get; private set; }

    public List<CustomerReturnLineSerial> Serials { get; private set; } = new();

    public void Update(decimal quantity, decimal unitPrice, string? batchNumber)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
        BatchNumber = batchNumber?.Trim();
    }

    public void AddSerial(string serialNumber)
    {
        Serials.Add(new CustomerReturnLineSerial(Id, Guard.NotNullOrWhiteSpace(serialNumber, nameof(serialNumber), maxLength: 128)));
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

public sealed class CustomerReturnLineSerial : Entity
{
    private CustomerReturnLineSerial() { }

    public CustomerReturnLineSerial(Guid customerReturnLineId, string serialNumber)
    {
        CustomerReturnLineId = customerReturnLineId;
        SerialNumber = serialNumber;
    }

    public Guid CustomerReturnLineId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
}

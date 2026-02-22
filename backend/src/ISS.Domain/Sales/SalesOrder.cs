using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum SalesOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Fulfilled = 2,
    Closed = 3,
    Cancelled = 4
}

public sealed class SalesOrder : AuditableEntity
{
    private SalesOrder() { }

    public SalesOrder(string number, Guid customerId, DateTimeOffset orderDate)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        CustomerId = customerId;
        OrderDate = orderDate;
        Status = SalesOrderStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public SalesOrderStatus Status { get; private set; }

    public List<SalesOrderLine> Lines { get; private set; } = new();

    public SalesOrderLine AddLine(Guid itemId, decimal quantity, decimal unitPrice)
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new DomainValidationException("Only draft sales orders can be edited.");
        }

        var line = new SalesOrderLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitPrice, nameof(unitPrice)));
        Lines.Add(line);
        return line;
    }

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new DomainValidationException("Only draft sales orders can be confirmed.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Sales order must have at least one line.");
        }

        Status = SalesOrderStatus.Confirmed;
    }

    public void MarkFulfilled()
    {
        if (Status is not (SalesOrderStatus.Confirmed or SalesOrderStatus.Fulfilled))
        {
            throw new DomainValidationException("Sales order must be confirmed before it can be fulfilled.");
        }

        Status = SalesOrderStatus.Fulfilled;
    }

    public void Close()
    {
        if (Status != SalesOrderStatus.Fulfilled)
        {
            throw new DomainValidationException("Only fulfilled sales orders can be closed.");
        }

        Status = SalesOrderStatus.Closed;
    }

    public void Cancel()
    {
        if (Status == SalesOrderStatus.Closed)
        {
            throw new DomainValidationException("Closed sales orders cannot be cancelled.");
        }

        Status = SalesOrderStatus.Cancelled;
    }
}

public sealed class SalesOrderLine : Entity
{
    private SalesOrderLine() { }

    public SalesOrderLine(Guid salesOrderId, Guid itemId, decimal quantity, decimal unitPrice)
    {
        SalesOrderId = salesOrderId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid SalesOrderId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}


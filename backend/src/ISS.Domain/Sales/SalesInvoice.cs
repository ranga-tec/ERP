using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum SalesInvoiceStatus
{
    Draft = 0,
    Posted = 1,
    Paid = 2,
    Voided = 3
}

public sealed class SalesInvoice : AuditableEntity
{
    private SalesInvoice() { }

    public SalesInvoice(string number, Guid customerId, DateTimeOffset invoiceDate, DateTimeOffset? dueDate)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        CustomerId = customerId;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        Status = SalesInvoiceStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset InvoiceDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public SalesInvoiceStatus Status { get; private set; }

    public List<SalesInvoiceLine> Lines { get; private set; } = new();

    public SalesInvoiceLine AddLine(Guid itemId, decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent)
    {
        if (Status != SalesInvoiceStatus.Draft)
        {
            throw new DomainValidationException("Only draft invoices can be edited.");
        }

        var line = new SalesInvoiceLine(
            Id,
            itemId,
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitPrice, nameof(unitPrice)),
            Guard.NotNegative(discountPercent, nameof(discountPercent)),
            Guard.NotNegative(taxPercent, nameof(taxPercent)));

        Lines.Add(line);
        return line;
    }

    public decimal Subtotal => Lines.Sum(l => l.LineSubtotal);
    public decimal TaxTotal => Lines.Sum(l => l.LineTax);
    public decimal Total => Lines.Sum(l => l.LineTotal);

    public void Post()
    {
        if (Status != SalesInvoiceStatus.Draft)
        {
            throw new DomainValidationException("Only draft invoices can be posted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Invoice must have at least one line.");
        }

        Status = SalesInvoiceStatus.Posted;
    }

    public void MarkPaid()
    {
        if (Status != SalesInvoiceStatus.Posted)
        {
            throw new DomainValidationException("Only posted invoices can be marked paid.");
        }

        Status = SalesInvoiceStatus.Paid;
    }

    public void Void()
    {
        if (Status == SalesInvoiceStatus.Voided)
        {
            return;
        }

        if (Status == SalesInvoiceStatus.Paid)
        {
            throw new DomainValidationException("Paid invoices cannot be voided.");
        }

        Status = SalesInvoiceStatus.Voided;
    }
}

public sealed class SalesInvoiceLine : Entity
{
    private SalesInvoiceLine() { }

    public SalesInvoiceLine(Guid salesInvoiceId, Guid itemId, decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent)
    {
        SalesInvoiceId = salesInvoiceId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
    }

    public Guid SalesInvoiceId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }

    public decimal LineSubtotal
    {
        get
        {
            var gross = Quantity * UnitPrice;
            var discount = gross * (DiscountPercent / 100m);
            return gross - discount;
        }
    }

    public decimal LineTax => LineSubtotal * (TaxPercent / 100m);
    public decimal LineTotal => LineSubtotal + LineTax;
}


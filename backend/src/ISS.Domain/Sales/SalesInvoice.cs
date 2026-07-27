using ISS.Domain.Common;
using ISS.Domain.Finance;

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
    public decimal DiscountPercent { get; private set; }
    public decimal DiscountAmount { get; private set; }

    public List<SalesInvoiceLine> Lines { get; private set; } = new();

    public SalesInvoiceLine AddLine(
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        Guid? revenueAccountId = null,
        Guid? materialRequisitionLineId = null,
        string? description = null)
    {
        EnsureDraftEditable();

        var line = new SalesInvoiceLine(
            Id,
            itemId,
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitPrice, nameof(unitPrice)),
            Guard.NotNegative(discountPercent, nameof(discountPercent)),
            Guard.NotNegative(taxPercent, nameof(taxPercent)),
            revenueAccountId,
            materialRequisitionLineId,
            description);

        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        Guid? revenueAccountId = null)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Invoice line not found.");

        line.Update(quantity, unitPrice, discountPercent, taxPercent, revenueAccountId);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Invoice line not found.");

        Lines.Remove(line);
    }

    /// <summary>
    /// Whole-invoice discount, entered either as a percentage of the line total or as a flat
    /// amount. Only one of the two is ever set; the other is cleared. Line-level discount is
    /// unaffected and is applied first.
    /// </summary>
    public void SetHeaderDiscount(decimal discountPercent, decimal discountAmount)
    {
        EnsureDraftEditable();
        Guard.NotNegative(discountPercent, nameof(discountPercent));
        Guard.NotNegative(discountAmount, nameof(discountAmount));

        if (discountPercent > 0m && discountAmount > 0m)
        {
            throw new DomainValidationException("Set the invoice discount as either a percentage or an amount, not both.");
        }

        if (discountPercent > 100m)
        {
            throw new DomainValidationException("Invoice discount percentage cannot exceed 100.");
        }

        DiscountPercent = discountPercent;
        DiscountAmount = discountAmount;
    }

    /// <summary>Sum of the lines after their own line discount, before the invoice discount.</summary>
    public decimal LinesSubtotal => Lines.Sum(l => l.LineSubtotal);

    /// <summary>
    /// The invoice discount in money. A percentage is resolved against the line total; a flat
    /// amount is capped at the line total so the invoice can never go negative.
    /// </summary>
    public decimal DiscountTotal
    {
        get
        {
            var linesSubtotal = LinesSubtotal;
            if (DiscountPercent > 0m)
            {
                return decimal.Round(linesSubtotal * (DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }

            return Math.Min(DiscountAmount, linesSubtotal);
        }
    }

    /// <summary>
    /// The invoice discount split across the lines in proportion to each line's subtotal, so tax
    /// is charged on the discounted amount at each line's own rate. Any rounding remainder lands
    /// on the largest line, keeping the parts equal to <see cref="DiscountTotal"/> exactly.
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal> AllocateDiscount()
    {
        var allocation = Lines.ToDictionary(l => l.Id, _ => 0m);
        var discount = DiscountTotal;
        var linesSubtotal = LinesSubtotal;
        if (discount <= 0m || linesSubtotal <= 0m)
        {
            return allocation;
        }

        var running = 0m;
        foreach (var line in Lines)
        {
            var share = decimal.Round(discount * (line.LineSubtotal / linesSubtotal), 2, MidpointRounding.AwayFromZero);
            allocation[line.Id] = share;
            running += share;
        }

        var remainder = discount - running;
        if (remainder != 0m)
        {
            var largest = Lines.OrderByDescending(l => l.LineSubtotal).First();
            allocation[largest.Id] += remainder;
        }

        return allocation;
    }

    /// <summary>Net of the invoice discount, which is what the customer is charged before tax.</summary>
    public decimal Subtotal => LinesSubtotal - DiscountTotal;

    public decimal TaxTotal
    {
        get
        {
            var allocation = AllocateDiscount();
            return Lines.Sum(l => (l.LineSubtotal - allocation[l.Id]) * (l.TaxPercent / 100m));
        }
    }

    public decimal Total => Subtotal + TaxTotal;

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

    private void EnsureDraftEditable()
    {
        if (Status != SalesInvoiceStatus.Draft)
        {
            throw new DomainValidationException("Only draft invoices can be edited.");
        }
    }
}

public sealed class SalesInvoiceLine : Entity
{
    private SalesInvoiceLine() { }

    public SalesInvoiceLine(
        Guid salesInvoiceId,
        Guid itemId,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        Guid? revenueAccountId = null,
        Guid? materialRequisitionLineId = null,
        string? description = null)
    {
        SalesInvoiceId = salesInvoiceId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        TaxPercent = taxPercent;
        RevenueAccountId = revenueAccountId;
        MaterialRequisitionLineId = materialRequisitionLineId;
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512);
    }

    public Guid SalesInvoiceId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public Guid? RevenueAccountId { get; private set; }
    public LedgerAccount? RevenueAccount { get; private set; }

    /// <summary>
    /// The material issued to the service job that this line bills, when the line was pulled from
    /// the job's issued materials. Lets a later invoice tell what has already been charged so the
    /// same issue is not billed twice.
    /// </summary>
    public Guid? MaterialRequisitionLineId { get; private set; }

    /// <summary>
    /// What the line is for, in the customer's words, when the item alone does not say it - a
    /// labour line billed against one service item needs to name the work, and a rolled-up line
    /// needs to say what it rolls up. Null falls back to the item name on the document.
    /// </summary>
    public string? Description { get; private set; }

    public void SetDescription(string? description)
        => Description = string.IsNullOrWhiteSpace(description)
            ? null
            : Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512);

    public void Update(decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent, Guid? revenueAccountId = null)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
        DiscountPercent = Guard.NotNegative(discountPercent, nameof(discountPercent));
        TaxPercent = Guard.NotNegative(taxPercent, nameof(taxPercent));
        RevenueAccountId = revenueAccountId;
    }

    public void AssignRevenueAccount(Guid? revenueAccountId) => RevenueAccountId = revenueAccountId;

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

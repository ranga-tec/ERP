using ISS.Domain.Common;

namespace ISS.Domain.Sales;

public enum SalesQuoteStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

public sealed class SalesQuote : AuditableEntity
{
    private SalesQuote() { }

    public SalesQuote(string number, Guid customerId, DateTimeOffset quoteDate, DateTimeOffset? validUntil)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        CustomerId = customerId;
        QuoteDate = quoteDate;
        ValidUntil = validUntil;
        Status = SalesQuoteStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateTimeOffset QuoteDate { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public SalesQuoteStatus Status { get; private set; }

    public List<SalesQuoteLine> Lines { get; private set; } = new();

    public SalesQuoteLine AddLine(Guid itemId, decimal quantity, decimal unitPrice)
    {
        EnsureDraftEditable();

        var line = new SalesQuoteLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), Guard.NotNegative(unitPrice, nameof(unitPrice)));
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, decimal unitPrice)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Quote line not found.");

        line.Update(quantity, unitPrice);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Quote line not found.");

        Lines.Remove(line);
    }

    private void EnsureDraftEditable()
    {
        if (Status != SalesQuoteStatus.Draft)
        {
            throw new DomainValidationException("Only draft quotes can be edited.");
        }
    }

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public void MarkSent()
    {
        if (Status != SalesQuoteStatus.Draft)
        {
            throw new DomainValidationException("Only draft quotes can be sent.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Quote must have at least one line.");
        }

        Status = SalesQuoteStatus.Sent;
    }

    public void Accept()
    {
        if (Status != SalesQuoteStatus.Sent)
        {
            throw new DomainValidationException("Only sent quotes can be accepted.");
        }

        Status = SalesQuoteStatus.Accepted;
    }

    public void Reject()
    {
        if (Status != SalesQuoteStatus.Sent)
        {
            throw new DomainValidationException("Only sent quotes can be rejected.");
        }

        Status = SalesQuoteStatus.Rejected;
    }
}

public sealed class SalesQuoteLine : Entity
{
    private SalesQuoteLine() { }

    public SalesQuoteLine(Guid salesQuoteId, Guid itemId, decimal quantity, decimal unitPrice)
    {
        SalesQuoteId = salesQuoteId;
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid SalesQuoteId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    public void Update(decimal quantity, decimal unitPrice)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
    }
}

using ISS.Domain.Common;

namespace ISS.Domain.Procurement;

public enum RequestForQuoteStatus
{
    Draft = 0,
    Sent = 1,
    Closed = 2,
    Cancelled = 3
}

public sealed class RequestForQuote : AuditableEntity
{
    private RequestForQuote() { }

    public RequestForQuote(string number, Guid supplierId, DateTimeOffset requestedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        SupplierId = supplierId;
        RequestedAt = requestedAt;
        Status = RequestForQuoteStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public RequestForQuoteStatus Status { get; private set; }

    public List<RequestForQuoteLine> Lines { get; private set; } = new();

    public RequestForQuoteLine AddLine(Guid itemId, decimal quantity, string? notes)
    {
        EnsureDraftEditable();

        var line = new RequestForQuoteLine(Id, itemId, Guard.Positive(quantity, nameof(quantity)), notes);
        Lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, string? notes)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("RFQ line not found.");

        line.Update(quantity, notes);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("RFQ line not found.");

        Lines.Remove(line);
    }

    private void EnsureDraftEditable()
    {
        if (Status != RequestForQuoteStatus.Draft)
        {
            throw new DomainValidationException("Only draft RFQs can be edited.");
        }
    }

    public void MarkSent()
    {
        if (Status != RequestForQuoteStatus.Draft)
        {
            throw new DomainValidationException("Only draft RFQs can be sent.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("RFQ must have at least one line.");
        }

        Status = RequestForQuoteStatus.Sent;
    }

    public void Close()
    {
        if (Status != RequestForQuoteStatus.Sent)
        {
            throw new DomainValidationException("Only sent RFQs can be closed.");
        }

        Status = RequestForQuoteStatus.Closed;
    }

    public void Cancel()
    {
        Status = RequestForQuoteStatus.Cancelled;
    }
}

public sealed class RequestForQuoteLine : Entity
{
    private RequestForQuoteLine() { }

    public RequestForQuoteLine(Guid requestForQuoteId, Guid itemId, decimal quantity, string? notes)
    {
        RequestForQuoteId = requestForQuoteId;
        ItemId = itemId;
        Quantity = quantity;
        Notes = notes?.Trim();
    }

    public Guid RequestForQuoteId { get; private set; }
    public Guid ItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    public void Update(decimal quantity, string? notes)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        Notes = notes?.Trim();
    }
}

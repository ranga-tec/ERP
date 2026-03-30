using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceEstimateStatus
{
    Draft = 0,
    Approved = 1,
    Rejected = 2
}

public enum ServiceEstimateCustomerApprovalStatus
{
    NotSent = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ServiceEstimateLineKind
{
    Part = 1,
    Labor = 2,
    Expense = 3
}

public sealed class ServiceEstimate : AuditableEntity
{
    private ServiceEstimate() { }

    public ServiceEstimate(
        string number,
        Guid serviceJobId,
        DateTimeOffset issuedAt,
        DateTimeOffset? validUntil,
        string? terms,
        Guid? revisedFromEstimateId = null,
        int revisionNumber = 0)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        ServiceJobId = serviceJobId;
        IssuedAt = issuedAt;
        ValidUntil = validUntil;
        Terms = terms?.Trim();
        RevisedFromEstimateId = revisedFromEstimateId;
        if (revisionNumber < 0)
        {
            throw new DomainValidationException("revisionNumber cannot be negative.");
        }

        RevisionNumber = revisionNumber;
        Status = ServiceEstimateStatus.Draft;
        CustomerApprovalStatus = ServiceEstimateCustomerApprovalStatus.NotSent;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public string? Terms { get; private set; }
    public Guid? RevisedFromEstimateId { get; private set; }
    public int RevisionNumber { get; private set; }
    public ServiceEstimateStatus Status { get; private set; }
    public ServiceEstimateCustomerApprovalStatus CustomerApprovalStatus { get; private set; }
    public DateTimeOffset? SentToCustomerAt { get; private set; }
    public DateTimeOffset? CustomerDecisionAt { get; private set; }

    public List<ServiceEstimateLine> Lines { get; private set; } = new();

    public ServiceEstimateLine AddLine(
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent)
    {
        EnsureDraftEditable();
        ResetPendingCustomerApproval();

        var line = new ServiceEstimateLine(
            Id,
            kind,
            itemId,
            Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512),
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitPrice, nameof(unitPrice)),
            Guard.NotNegative(taxPercent, nameof(taxPercent)));

        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent)
    {
        EnsureDraftEditable();
        ResetPendingCustomerApproval();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service estimate line not found.");

        line.Update(kind, itemId, description, quantity, unitPrice, taxPercent);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();
        ResetPendingCustomerApproval();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service estimate line not found.");

        Lines.Remove(line);
    }

    public decimal Subtotal => Lines.Sum(l => l.LineSubtotal);
    public decimal TaxTotal => Lines.Sum(l => l.LineTax);
    public decimal Total => Lines.Sum(l => l.LineTotal);

    public void Approve()
        => Approve(DateTimeOffset.UtcNow);

    public void Approve(DateTimeOffset decisionAt)
    {
        if (Status != ServiceEstimateStatus.Draft)
        {
            throw new DomainValidationException("Only draft estimates can be approved.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Estimate must have at least one line.");
        }

        Status = ServiceEstimateStatus.Approved;
        CustomerApprovalStatus = ServiceEstimateCustomerApprovalStatus.Approved;
        CustomerDecisionAt = decisionAt;
    }

    public void Reject()
        => Reject(DateTimeOffset.UtcNow);

    public void Reject(DateTimeOffset decisionAt)
    {
        if (Status != ServiceEstimateStatus.Draft)
        {
            throw new DomainValidationException("Only draft estimates can be rejected.");
        }

        Status = ServiceEstimateStatus.Rejected;
        CustomerApprovalStatus = ServiceEstimateCustomerApprovalStatus.Rejected;
        CustomerDecisionAt = decisionAt;
    }

    public void UpdateHeader(DateTimeOffset? validUntil, string? terms)
    {
        EnsureDraftEditable();
        ResetPendingCustomerApproval();
        ValidUntil = validUntil;
        Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim();
    }

    public void MarkSentToCustomer(DateTimeOffset sentAt)
    {
        EnsureDraftEditable();
        CustomerApprovalStatus = ServiceEstimateCustomerApprovalStatus.Pending;
        SentToCustomerAt = sentAt;
        CustomerDecisionAt = null;
    }

    private void EnsureDraftEditable()
    {
        if (Status != ServiceEstimateStatus.Draft)
        {
            throw new DomainValidationException("Only draft estimates can be edited.");
        }
    }

    private void ResetPendingCustomerApproval()
    {
        if (CustomerApprovalStatus != ServiceEstimateCustomerApprovalStatus.Pending)
        {
            return;
        }

        CustomerApprovalStatus = ServiceEstimateCustomerApprovalStatus.NotSent;
        SentToCustomerAt = null;
        CustomerDecisionAt = null;
    }
}

public sealed class ServiceEstimateLine : Entity
{
    private ServiceEstimateLine() { }

    public ServiceEstimateLine(
        Guid serviceEstimateId,
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent)
    {
        if (kind == ServiceEstimateLineKind.Part && itemId is null)
        {
            throw new DomainValidationException("Part estimate lines require an item.");
        }

        ServiceEstimateId = serviceEstimateId;
        Kind = kind;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxPercent = taxPercent;
    }

    public Guid ServiceEstimateId { get; private set; }
    public ServiceEstimateLineKind Kind { get; private set; }
    public Guid? ItemId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxPercent { get; private set; }

    public void Update(
        ServiceEstimateLineKind kind,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent)
    {
        if (kind == ServiceEstimateLineKind.Part && itemId is null)
        {
            throw new DomainValidationException("Part estimate lines require an item.");
        }

        Kind = kind;
        ItemId = itemId;
        Description = Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512);
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.NotNegative(unitPrice, nameof(unitPrice));
        TaxPercent = Guard.NotNegative(taxPercent, nameof(taxPercent));
    }

    public decimal LineSubtotal => Quantity * UnitPrice;
    public decimal LineTax => LineSubtotal * (TaxPercent / 100m);
    public decimal LineTotal => LineSubtotal + LineTax;
}

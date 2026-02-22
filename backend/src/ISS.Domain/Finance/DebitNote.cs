using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public sealed class DebitNote : AuditableEntity
{
    private DebitNote() { }

    public DebitNote(
        string referenceNumber,
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        DateTimeOffset issuedAt,
        string? notes,
        string? sourceReferenceType,
        Guid? sourceReferenceId)
    {
        ReferenceNumber = Guard.NotNullOrWhiteSpace(referenceNumber, nameof(ReferenceNumber), maxLength: 64);
        CounterpartyType = counterpartyType;
        CounterpartyId = counterpartyId;
        Amount = Guard.Positive(amount, nameof(amount));
        IssuedAt = issuedAt;
        Notes = notes?.Trim();
        SourceReferenceType = string.IsNullOrWhiteSpace(sourceReferenceType)
            ? null
            : Guard.NotNullOrWhiteSpace(sourceReferenceType, nameof(sourceReferenceType), maxLength: 64);
        SourceReferenceId = sourceReferenceId;
    }

    public string ReferenceNumber { get; private set; } = null!;
    public CounterpartyType CounterpartyType { get; private set; }
    public Guid CounterpartyId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public string? Notes { get; private set; }
    public string? SourceReferenceType { get; private set; }
    public Guid? SourceReferenceId { get; private set; }
}


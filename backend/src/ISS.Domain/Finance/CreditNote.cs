using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public sealed class CreditNote : AuditableEntity
{
    private CreditNote() { }

    public CreditNote(
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
        RemainingAmount = Amount;
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
    public decimal RemainingAmount { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public string? Notes { get; private set; }
    public string? SourceReferenceType { get; private set; }
    public Guid? SourceReferenceId { get; private set; }

    public List<CreditNoteAllocation> Allocations { get; private set; } = new();

    public CreditNoteAllocation AllocateToAr(Guid arEntryId, decimal amount)
    {
        var allocated = Guard.Positive(amount, nameof(amount));
        EnsureRemaining(allocated);
        RemainingAmount -= allocated;

        var allocation = new CreditNoteAllocation(Id, arEntryId, null, allocated);
        Allocations.Add(allocation);
        return allocation;
    }

    public CreditNoteAllocation AllocateToAp(Guid apEntryId, decimal amount)
    {
        var allocated = Guard.Positive(amount, nameof(amount));
        EnsureRemaining(allocated);
        RemainingAmount -= allocated;

        var allocation = new CreditNoteAllocation(Id, null, apEntryId, allocated);
        Allocations.Add(allocation);
        return allocation;
    }

    private void EnsureRemaining(decimal allocateAmount)
    {
        if (allocateAmount > RemainingAmount)
        {
            throw new DomainValidationException("Allocation exceeds remaining credit amount.");
        }
    }
}

public sealed class CreditNoteAllocation : Entity
{
    private CreditNoteAllocation() { }

    public CreditNoteAllocation(Guid creditNoteId, Guid? accountsReceivableEntryId, Guid? accountsPayableEntryId, decimal amount)
    {
        CreditNoteId = creditNoteId;
        AccountsReceivableEntryId = accountsReceivableEntryId;
        AccountsPayableEntryId = accountsPayableEntryId;
        Amount = amount;
    }

    public Guid CreditNoteId { get; private set; }
    public Guid? AccountsReceivableEntryId { get; private set; }
    public Guid? AccountsPayableEntryId { get; private set; }
    public decimal Amount { get; private set; }
}


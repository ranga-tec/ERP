using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PaymentDirection
{
    Incoming = 1,
    Outgoing = 2
}

public enum CounterpartyType
{
    Customer = 1,
    Supplier = 2
}

public sealed class Payment : AuditableEntity
{
    private Payment() { }

    public Payment(
        string referenceNumber,
        PaymentDirection direction,
        CounterpartyType counterpartyType,
        Guid counterpartyId,
        decimal amount,
        DateTimeOffset paidAt,
        string? notes)
    {
        ReferenceNumber = Guard.NotNullOrWhiteSpace(referenceNumber, nameof(ReferenceNumber), maxLength: 64);
        Direction = direction;
        CounterpartyType = counterpartyType;
        CounterpartyId = counterpartyId;
        Amount = Guard.Positive(amount, nameof(Amount));
        PaidAt = paidAt;
        Notes = notes?.Trim();
    }

    public string ReferenceNumber { get; private set; } = null!;
    public PaymentDirection Direction { get; private set; }
    public CounterpartyType CounterpartyType { get; private set; }
    public Guid CounterpartyId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public string? Notes { get; private set; }

    public List<PaymentAllocation> Allocations { get; private set; } = new();

    public PaymentAllocation AllocateToAr(Guid arEntryId, decimal amount)
    {
        var allocation = new PaymentAllocation(Id, arEntryId, null, Guard.Positive(amount, nameof(amount)));
        Allocations.Add(allocation);
        return allocation;
    }

    public PaymentAllocation AllocateToAp(Guid apEntryId, decimal amount)
    {
        var allocation = new PaymentAllocation(Id, null, apEntryId, Guard.Positive(amount, nameof(amount)));
        Allocations.Add(allocation);
        return allocation;
    }
}

public sealed class PaymentAllocation : Entity
{
    private PaymentAllocation() { }

    public PaymentAllocation(Guid paymentId, Guid? accountsReceivableEntryId, Guid? accountsPayableEntryId, decimal amount)
    {
        PaymentId = paymentId;
        AccountsReceivableEntryId = accountsReceivableEntryId;
        AccountsPayableEntryId = accountsPayableEntryId;
        Amount = amount;
    }

    public Guid PaymentId { get; private set; }
    public Guid? AccountsReceivableEntryId { get; private set; }
    public Guid? AccountsPayableEntryId { get; private set; }
    public decimal Amount { get; private set; }
}

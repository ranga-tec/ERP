using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public sealed class AccountsPayableEntry : AuditableEntity
{
    private AccountsPayableEntry() { }

    public AccountsPayableEntry(Guid supplierId, string referenceType, Guid referenceId, decimal amount, DateTimeOffset postedAt)
    {
        SupplierId = supplierId;
        ReferenceType = Guard.NotNullOrWhiteSpace(referenceType, nameof(referenceType), maxLength: 64);
        ReferenceId = referenceId;
        Amount = Guard.Positive(amount, nameof(amount));
        Outstanding = Amount;
        PostedAt = postedAt;
    }

    public Guid SupplierId { get; private set; }
    public string ReferenceType { get; private set; } = null!;
    public Guid ReferenceId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Outstanding { get; private set; }
    public DateTimeOffset PostedAt { get; private set; }

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("Payment amount must be positive.");
        }

        if (amount > Outstanding)
        {
            throw new DomainValidationException("Payment amount cannot exceed outstanding amount.");
        }

        Outstanding -= amount;
    }
}

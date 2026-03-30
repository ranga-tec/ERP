using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashTransactionType
{
    OpeningBalance = 1,
    TopUp = 2,
    ExpenseSettlement = 3,
    Adjustment = 4
}

public enum PettyCashTransactionDirection
{
    In = 1,
    Out = 2
}

public sealed class PettyCashFund : AuditableEntity
{
    private PettyCashFund() { }

    public PettyCashFund(
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        CurrencyCode = Guard.NotNullOrWhiteSpace(currencyCode, nameof(currencyCode), maxLength: 3).ToUpperInvariant();
        CustodianName = NormalizeOptional(custodianName, nameof(custodianName), 128);
        Notes = NormalizeOptional(notes, nameof(notes), 512);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = "USD";
    public string? CustodianName { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public List<PettyCashTransaction> Transactions { get; private set; } = new();

    public decimal Balance => Transactions.Sum(x => x.SignedAmount);

    public void Update(
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes,
        bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        CurrencyCode = Guard.NotNullOrWhiteSpace(currencyCode, nameof(currencyCode), maxLength: 3).ToUpperInvariant();
        CustodianName = NormalizeOptional(custodianName, nameof(custodianName), 128);
        Notes = NormalizeOptional(notes, nameof(notes), 512);
        IsActive = isActive;
    }

    public PettyCashTransaction AddOpeningBalance(
        decimal amount,
        DateTimeOffset occurredAt,
        string? referenceNumber,
        string? notes)
        => AddTransaction(
            PettyCashTransactionType.OpeningBalance,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: null,
            referenceId: null,
            referenceNumber,
            notes);

    public PettyCashTransaction AddTopUp(
        decimal amount,
        DateTimeOffset occurredAt,
        string? referenceNumber,
        string? notes)
        => AddTransaction(
            PettyCashTransactionType.TopUp,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: null,
            referenceId: null,
            referenceNumber,
            notes);

    public PettyCashTransaction RecordExpenseSettlement(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid referenceId,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();
        EnsureSufficientBalance(amount);

        return AddTransaction(
            PettyCashTransactionType.ExpenseSettlement,
            PettyCashTransactionDirection.Out,
            amount,
            occurredAt,
            referenceType: "SEC",
            referenceId: referenceId,
            referenceNumber,
            notes);
    }

    public PettyCashTransaction AddAdjustment(
        decimal amount,
        PettyCashTransactionDirection direction,
        DateTimeOffset occurredAt,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();
        if (direction == PettyCashTransactionDirection.Out)
        {
            EnsureSufficientBalance(amount);
        }

        return AddTransaction(
            PettyCashTransactionType.Adjustment,
            direction,
            amount,
            occurredAt,
            referenceType: null,
            referenceId: null,
            referenceNumber,
            notes);
    }

    private PettyCashTransaction AddTransaction(
        PettyCashTransactionType type,
        PettyCashTransactionDirection direction,
        decimal amount,
        DateTimeOffset occurredAt,
        string? referenceType,
        Guid? referenceId,
        string? referenceNumber,
        string? notes)
    {
        var transaction = new PettyCashTransaction(
            Id,
            type,
            direction,
            Guard.Positive(amount, nameof(amount)),
            occurredAt,
            NormalizeOptional(referenceType, nameof(referenceType), 64),
            referenceId,
            NormalizeOptional(referenceNumber, nameof(referenceNumber), 128),
            NormalizeOptional(notes, nameof(notes), 512));

        Transactions.Add(transaction);
        return transaction;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainValidationException("Inactive petty cash funds cannot receive new transactions.");
        }
    }

    private void EnsureSufficientBalance(decimal amount)
    {
        if (Balance < amount)
        {
            throw new DomainValidationException("Petty cash fund does not have enough balance for this transaction.");
        }
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(value, paramName, maxLength: maxLength);
    }
}

public sealed class PettyCashTransaction : Entity
{
    private PettyCashTransaction() { }

    public PettyCashTransaction(
        Guid pettyCashFundId,
        PettyCashTransactionType type,
        PettyCashTransactionDirection direction,
        decimal amount,
        DateTimeOffset occurredAt,
        string? referenceType,
        Guid? referenceId,
        string? referenceNumber,
        string? notes)
    {
        PettyCashFundId = pettyCashFundId;
        Type = type;
        Direction = direction;
        Amount = Guard.Positive(amount, nameof(amount));
        OccurredAt = occurredAt;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceNumber = referenceNumber;
        Notes = notes;
    }

    public Guid PettyCashFundId { get; private set; }
    public PettyCashTransactionType Type { get; private set; }
    public PettyCashTransactionDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public decimal SignedAmount => Direction == PettyCashTransactionDirection.In ? Amount : -Amount;
}

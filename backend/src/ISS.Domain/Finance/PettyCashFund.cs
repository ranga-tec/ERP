using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashTransactionType
{
    OpeningBalance = 1,
    TopUp = 2,
    ExpenseSettlement = 3,
    Adjustment = 4,
    IouRelease = 5,
    IouSettlement = 6
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

    public PettyCashTransaction RecordIouRelease(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid iouId,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();
        EnsureSufficientBalance(amount);

        return AddTransaction(
            PettyCashTransactionType.IouRelease,
            PettyCashTransactionDirection.Out,
            amount,
            occurredAt,
            referenceType: "IOU",
            referenceId: iouId,
            referenceNumber,
            notes);
    }

    public PettyCashTransaction RecordIouSettlement(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid iouId,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();

        return AddTransaction(
            PettyCashTransactionType.IouSettlement,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: "IOU",
            referenceId: iouId,
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

public enum PettyCashIouStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Released = 3,
    Settled = 4,
    Rejected = 5,
    Cancelled = 6
}

public sealed class PettyCashIou : AuditableEntity
{
    private PettyCashIou() { }

    public PettyCashIou(
        string number,
        Guid serviceJobId,
        Guid requestedByUserId,
        string requestedByName,
        decimal amount,
        string purpose,
        DateTimeOffset requestedAt,
        DateTimeOffset? expectedSettlementAt,
        Guid? serviceJobDailySheetId = null)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        ServiceJobId = serviceJobId;
        ServiceJobDailySheetId = serviceJobDailySheetId;
        RequestedByUserId = requestedByUserId;
        RequestedByName = Guard.NotNullOrWhiteSpace(requestedByName, nameof(requestedByName), maxLength: 256);
        Amount = Guard.Positive(amount, nameof(amount));
        Purpose = Guard.NotNullOrWhiteSpace(purpose, nameof(purpose), maxLength: 1000);
        RequestedAt = requestedAt;
        ExpectedSettlementAt = expectedSettlementAt;
        Status = PettyCashIouStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public Guid? ServiceJobDailySheetId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Purpose { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ExpectedSettlementAt { get; private set; }
    public PettyCashIouStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? PettyCashFundId { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public string? ReleaseReference { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public decimal? SettledAmount { get; private set; }
    public string? SettlementReference { get; private set; }

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != PettyCashIouStatus.Draft)
        {
            throw new DomainValidationException("Only draft IOUs can be submitted.");
        }

        Status = PettyCashIouStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashIouStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted IOUs can be approved.");
        }

        Status = PettyCashIouStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? rejectionReason)
    {
        if (Status != PettyCashIouStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted IOUs can be rejected.");
        }

        Status = PettyCashIouStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
            ? null
            : Guard.NotNullOrWhiteSpace(rejectionReason, nameof(rejectionReason), maxLength: 512);
    }

    public void Release(Guid pettyCashFundId, DateTimeOffset releasedAt, string? releaseReference)
    {
        if (Status != PettyCashIouStatus.Approved)
        {
            throw new DomainValidationException("Only approved IOUs can be released.");
        }

        PettyCashFundId = pettyCashFundId;
        ReleasedAt = releasedAt;
        ReleaseReference = string.IsNullOrWhiteSpace(releaseReference) ? null : Guard.NotNullOrWhiteSpace(releaseReference, nameof(releaseReference), maxLength: 128);
        Status = PettyCashIouStatus.Released;
    }

    public void Settle(decimal settledAmount, DateTimeOffset settledAt, string? settlementReference)
    {
        if (Status != PettyCashIouStatus.Released)
        {
            throw new DomainValidationException("Only released IOUs can be settled.");
        }

        SettledAmount = Guard.NotNegative(settledAmount, nameof(settledAmount));
        SettledAt = settledAt;
        SettlementReference = string.IsNullOrWhiteSpace(settlementReference) ? null : Guard.NotNullOrWhiteSpace(settlementReference, nameof(settlementReference), maxLength: 128);
        Status = PettyCashIouStatus.Settled;
    }

    public void Cancel()
    {
        if (Status is PettyCashIouStatus.Released or PettyCashIouStatus.Settled)
        {
            throw new DomainValidationException("Released or settled IOUs cannot be cancelled.");
        }

        Status = PettyCashIouStatus.Cancelled;
    }
}

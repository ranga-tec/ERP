using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashTransactionType
{
    OpeningBalance = 1,
    TopUp = 2,
    ExpenseSettlement = 3,
    Adjustment = 4,
    IouRelease = 5,
    IouSettlement = 6,
    RequestFunding = 7,
    HeadOfficeReturn = 8,
    CategoryTransferOut = 9,
    CategoryTransferIn = 10,
    HeadOfficeIouFunding = 11,
    FundReplenishment = 12,
    FundReturn = 13
}

public enum PettyCashTransactionDirection
{
    In = 1,
    Out = 2
}

public enum PettyCashCashCountFrequency
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    ShiftClose = 3
}

public sealed class PettyCashFund : AuditableEntity
{
    private PettyCashFund() { }

    public PettyCashFund(
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes,
        string? location = null,
        decimal authorizedFloat = 0m,
        decimal transactionLimit = 0m,
        decimal advanceLimit = 0m,
        bool requireReceipt = true,
        bool blockOverdueAdvances = true,
        Guid? settlementShortageExpenseAccountId = null,
        string? settlementShortageCostCenterCode = null,
        PettyCashCashCountFrequency cashCountFrequency = PettyCashCashCountFrequency.Weekly,
        DateTimeOffset? nextCashCountDueAt = null)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        CurrencyCode = Guard.NotNullOrWhiteSpace(currencyCode, nameof(currencyCode), maxLength: 3).ToUpperInvariant();
        CustodianName = NormalizeOptional(custodianName, nameof(custodianName), 128);
        Notes = NormalizeOptional(notes, nameof(notes), 512);
        ApplyControls(
            location,
            authorizedFloat,
            transactionLimit,
            advanceLimit,
            requireReceipt,
            blockOverdueAdvances,
            settlementShortageExpenseAccountId,
            settlementShortageCostCenterCode,
            cashCountFrequency,
            nextCashCountDueAt);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = "USD";
    public string? CustodianName { get; private set; }
    public string? Location { get; private set; }
    public string? Notes { get; private set; }
    /// <summary>The management-approved accountability ceiling for this site float. Zero means unset.</summary>
    public decimal AuthorizedFloat { get; private set; }
    /// <summary>Maximum value of one direct petty-cash expense. Zero means no configured limit.</summary>
    public decimal TransactionLimit { get; private set; }
    /// <summary>Maximum value of one employee advance. Zero means no configured limit.</summary>
    public decimal AdvanceLimit { get; private set; }
    public bool RequireReceipt { get; private set; }
    public bool BlockOverdueAdvances { get; private set; }
    public Guid? SettlementShortageExpenseAccountId { get; private set; }
    public LedgerAccount? SettlementShortageExpenseAccount { get; private set; }
    public string? SettlementShortageCostCenterCode { get; private set; }
    public PettyCashCashCountFrequency CashCountFrequency { get; private set; }
    public DateTimeOffset? LastCashCountAt { get; private set; }
    public DateTimeOffset? NextCashCountDueAt { get; private set; }
    public bool IsActive { get; private set; }

    public List<PettyCashTransaction> Transactions { get; private set; } = new();
    public List<PettyCashCashCount> CashCounts { get; private set; } = new();

    public decimal Balance => Transactions.Sum(x => x.SignedAmount);

    /// <summary>
    /// What is left of one funded category. The custodian keeps sub-accounts inside a single float,
    /// so a category balance is this same ledger filtered to the request line that funded it -
    /// money in from head office, money out as advances and vouchers charged back to that category.
    /// </summary>
    public decimal BalanceForRequestLine(Guid pettyCashRequestLineId)
        => Transactions
            .Where(x => x.PettyCashRequestLineId == pettyCashRequestLineId)
            .Sum(x => x.SignedAmount);

    public void Update(
        string code,
        string name,
        string currencyCode,
        string? custodianName,
        string? notes,
        bool isActive,
        string? location = null,
        decimal authorizedFloat = 0m,
        decimal transactionLimit = 0m,
        decimal advanceLimit = 0m,
        bool requireReceipt = true,
        bool blockOverdueAdvances = true,
        Guid? settlementShortageExpenseAccountId = null,
        string? settlementShortageCostCenterCode = null,
        PettyCashCashCountFrequency cashCountFrequency = PettyCashCashCountFrequency.Weekly,
        DateTimeOffset? nextCashCountDueAt = null)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        CurrencyCode = Guard.NotNullOrWhiteSpace(currencyCode, nameof(currencyCode), maxLength: 3).ToUpperInvariant();
        CustodianName = NormalizeOptional(custodianName, nameof(custodianName), 128);
        Notes = NormalizeOptional(notes, nameof(notes), 512);
        ApplyControls(
            location,
            authorizedFloat,
            transactionLimit,
            advanceLimit,
            requireReceipt,
            blockOverdueAdvances,
            settlementShortageExpenseAccountId,
            settlementShortageCostCenterCode,
            cashCountFrequency,
            nextCashCountDueAt);
        IsActive = isActive;
    }

    public void MarkCashCountApproved(DateTimeOffset countedAt)
    {
        LastCashCountAt = countedAt;
        NextCashCountDueAt = CashCountFrequency switch
        {
            PettyCashCashCountFrequency.Daily => countedAt.AddDays(1),
            PettyCashCashCountFrequency.Weekly => countedAt.AddDays(7),
            PettyCashCashCountFrequency.ShiftClose => countedAt.AddHours(8),
            _ => null,
        };
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
    {
        EnsureActive();

        return AddTransaction(
            PettyCashTransactionType.TopUp,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: null,
            referenceId: null,
            referenceNumber,
            notes);
    }

    public PettyCashTransaction RecordExpenseSettlement(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid referenceId,
        string? referenceNumber,
        string? notes,
        Guid? pettyCashRequestLineId = null)
    {
        EnsureActive();
        EnsureWithinTransactionLimit(amount);
        EnsureSufficientBalance(amount);

        return AddTransaction(
            PettyCashTransactionType.ExpenseSettlement,
            PettyCashTransactionDirection.Out,
            amount,
            occurredAt,
            referenceType: "SEC",
            referenceId: referenceId,
            referenceNumber,
            notes,
            pettyCashRequestLineId);
    }

    /// <summary>
    /// Replenishes the physical float independently from expense or IOU approval. New V2 requests
    /// use this fund-level transaction; category-linked request funding remains only for history.
    /// </summary>
    public PettyCashTransaction RecordFundReplenishment(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid replenishmentRequestId,
        string referenceNumber,
        string? notes)
    {
        EnsureActive();

        return AddTransaction(
            PettyCashTransactionType.FundReplenishment,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: "PCR",
            referenceId: replenishmentRequestId,
            referenceNumber: Guard.NotNullOrWhiteSpace(referenceNumber, nameof(referenceNumber), maxLength: 128),
            notes);
    }

    /// <summary>
    /// Head office paying out one line of a petty cash request. Carries the request line so the
    /// category's sub-balance can be read straight off the ledger.
    /// </summary>
    public PettyCashTransaction RecordRequestFunding(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid requestId,
        Guid requestLineId,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();

        return AddTransaction(
            PettyCashTransactionType.RequestFunding,
            PettyCashTransactionDirection.In,
            amount,
            occurredAt,
            referenceType: "PCR",
            referenceId: requestId,
            referenceNumber,
            notes,
            pettyCashRequestLineId: requestLineId);
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
        string? notes,
        Guid? pettyCashRequestLineId = null)
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
            notes,
            pettyCashRequestLineId);
    }

    /// <summary>Returns reconciled physical cash to head office without a category sub-ledger.</summary>
    public PettyCashTransaction RecordFundReturn(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid pettyCashReturnId,
        string receiptReference,
        string? notes)
    {
        EnsureActive();
        EnsureSufficientBalance(amount);

        return AddTransaction(
            PettyCashTransactionType.FundReturn,
            PettyCashTransactionDirection.Out,
            amount,
            occurredAt,
            referenceType: "PCRTN",
            referenceId: pettyCashReturnId,
            referenceNumber: Guard.NotNullOrWhiteSpace(receiptReference, nameof(receiptReference), maxLength: 128),
            notes);
    }

    public PettyCashTransaction RecordHeadOfficeIouFunding(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid iouId,
        string? referenceNumber,
        string? notes)
    {
        EnsureActive();

        return AddTransaction(
            PettyCashTransactionType.HeadOfficeIouFunding,
            PettyCashTransactionDirection.In,
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
        string? notes,
        Guid? pettyCashRequestLineId = null)
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
            notes,
            pettyCashRequestLineId);
    }

    /// <summary>
    /// Cash from one funded category that head office has physically received back. This is an
    /// asset/custody movement, not an expense, and therefore reduces both the float and the same
    /// category sub-account that the original request funding credited.
    /// </summary>
    public PettyCashTransaction RecordHeadOfficeReturn(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid pettyCashReturnId,
        Guid pettyCashRequestLineId,
        string receiptReference,
        string? notes)
    {
        EnsureActive();
        EnsureSufficientBalance(amount);

        var categoryBalance = BalanceForRequestLine(pettyCashRequestLineId);
        if (categoryBalance < amount)
        {
            throw new DomainValidationException(
                $"The selected petty cash category has only {categoryBalance:0.00} available to return.");
        }

        return AddTransaction(
            PettyCashTransactionType.HeadOfficeReturn,
            PettyCashTransactionDirection.Out,
            amount,
            occurredAt,
            referenceType: "PCRTN",
            referenceId: pettyCashReturnId,
            referenceNumber: receiptReference,
            notes,
            pettyCashRequestLineId);
    }

    /// <summary>
    /// Reclassifies cash between two funded categories without changing the physical fund total.
    /// The equal out/in entries deliberately share one reference so the transfer remains atomic and
    /// auditable while each request-line sub-account keeps the correct balance.
    /// </summary>
    public IReadOnlyList<PettyCashTransaction> RecordCategoryReallocation(
        decimal amount,
        DateTimeOffset occurredAt,
        Guid pettyCashReallocationId,
        Guid sourcePettyCashRequestLineId,
        Guid destinationPettyCashRequestLineId,
        string referenceNumber,
        string reason)
    {
        EnsureActive();
        if (sourcePettyCashRequestLineId == destinationPettyCashRequestLineId)
        {
            throw new DomainValidationException("Source and destination categories must be different.");
        }

        var validatedAmount = Guard.Positive(amount, nameof(amount));
        var transactionNote = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim()[..Math.Min(reason.Trim().Length, 512)];
        var sourceBalance = BalanceForRequestLine(sourcePettyCashRequestLineId);
        if (sourceBalance < validatedAmount)
        {
            throw new DomainValidationException(
                $"The source petty cash category has only {sourceBalance:0.00} available to reallocate.");
        }

        var transferOut = AddTransaction(
            PettyCashTransactionType.CategoryTransferOut,
            PettyCashTransactionDirection.Out,
            validatedAmount,
            occurredAt,
            referenceType: "PCRAL",
            referenceId: pettyCashReallocationId,
            referenceNumber,
            transactionNote,
            sourcePettyCashRequestLineId);
        var transferIn = AddTransaction(
            PettyCashTransactionType.CategoryTransferIn,
            PettyCashTransactionDirection.In,
            validatedAmount,
            occurredAt,
            referenceType: "PCRAL",
            referenceId: pettyCashReallocationId,
            referenceNumber,
            transactionNote,
            destinationPettyCashRequestLineId);

        return [transferOut, transferIn];
    }

    private PettyCashTransaction AddTransaction(
        PettyCashTransactionType type,
        PettyCashTransactionDirection direction,
        decimal amount,
        DateTimeOffset occurredAt,
        string? referenceType,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        Guid? pettyCashRequestLineId = null)
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
            NormalizeOptional(notes, nameof(notes), 512),
            pettyCashRequestLineId);

        Transactions.Add(transaction);
        return transaction;
    }

    private void ApplyControls(
        string? location,
        decimal authorizedFloat,
        decimal transactionLimit,
        decimal advanceLimit,
        bool requireReceipt,
        bool blockOverdueAdvances,
        Guid? settlementShortageExpenseAccountId,
        string? settlementShortageCostCenterCode,
        PettyCashCashCountFrequency cashCountFrequency,
        DateTimeOffset? nextCashCountDueAt)
    {
        Location = NormalizeOptional(location, nameof(location), 128);
        AuthorizedFloat = Guard.NotNegative(authorizedFloat, nameof(authorizedFloat));
        TransactionLimit = Guard.NotNegative(transactionLimit, nameof(transactionLimit));
        AdvanceLimit = Guard.NotNegative(advanceLimit, nameof(advanceLimit));
        RequireReceipt = requireReceipt;
        BlockOverdueAdvances = blockOverdueAdvances;
        SettlementShortageExpenseAccountId = settlementShortageExpenseAccountId;
        SettlementShortageCostCenterCode = NormalizeOptional(
            settlementShortageCostCenterCode,
            nameof(settlementShortageCostCenterCode),
            64)?.ToUpperInvariant();
        CashCountFrequency = cashCountFrequency;
        NextCashCountDueAt = cashCountFrequency == PettyCashCashCountFrequency.None
            ? null
            : nextCashCountDueAt ?? NextCashCountDueAt;
    }

    private void EnsureWithinTransactionLimit(decimal amount)
    {
        if (TransactionLimit > 0m && amount > TransactionLimit)
        {
            throw new DomainValidationException(
                $"This petty cash transaction exceeds the configured limit of {TransactionLimit:0.00}. Use procurement or obtain a revised limit.");
        }
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

public enum PettyCashCashCountStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public sealed class PettyCashCashCount : AuditableEntity
{
    private PettyCashCashCount() { }

    public PettyCashCashCount(
        string number,
        Guid pettyCashFundId,
        DateTimeOffset countedAt,
        Guid countedByUserId,
        string countedByName,
        decimal physicalCash,
        decimal outstandingAdvances,
        decimal supportedExpenseVouchers,
        decimal authorizedFloatSnapshot,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        PettyCashFundId = pettyCashFundId == Guid.Empty
            ? throw new DomainValidationException("Petty cash fund is required.")
            : pettyCashFundId;
        CountedAt = countedAt;
        CountedByUserId = countedByUserId == Guid.Empty
            ? throw new DomainValidationException("The cash counter is required.")
            : countedByUserId;
        CountedByName = Guard.NotNullOrWhiteSpace(countedByName, nameof(countedByName), maxLength: 256);
        PhysicalCash = Guard.NotNegative(physicalCash, nameof(physicalCash));
        OutstandingAdvances = Guard.NotNegative(outstandingAdvances, nameof(outstandingAdvances));
        SupportedExpenseVouchers = Guard.NotNegative(supportedExpenseVouchers, nameof(supportedExpenseVouchers));
        AuthorizedFloatSnapshot = Guard.NotNegative(authorizedFloatSnapshot, nameof(authorizedFloatSnapshot));
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.NotNullOrWhiteSpace(notes, nameof(notes), maxLength: 1000);
        Status = PettyCashCashCountStatus.Submitted;
    }

    public string Number { get; private set; } = null!;
    public Guid PettyCashFundId { get; private set; }
    public DateTimeOffset CountedAt { get; private set; }
    public Guid CountedByUserId { get; private set; }
    public string CountedByName { get; private set; } = null!;
    public decimal PhysicalCash { get; private set; }
    public decimal OutstandingAdvances { get; private set; }
    public decimal SupportedExpenseVouchers { get; private set; }
    public decimal AuthorizedFloatSnapshot { get; private set; }
    public string? Notes { get; private set; }
    public PettyCashCashCountStatus Status { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public decimal Accountability => PhysicalCash + OutstandingAdvances + SupportedExpenseVouchers;
    public decimal Variance => Accountability - AuthorizedFloatSnapshot;

    public void Approve(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashCashCountStatus.Submitted)
        {
            throw new DomainValidationException("Only a submitted cash count can be approved.");
        }

        if (approvedByUserId == CountedByUserId)
        {
            throw new DomainValidationException("The cash counter cannot approve their own count.");
        }

        ApprovedByUserId = approvedByUserId == Guid.Empty
            ? throw new DomainValidationException("The cash-count approver is required.")
            : approvedByUserId;
        ApprovedAt = approvedAt;
        Status = PettyCashCashCountStatus.Approved;
    }

    public void Reject(Guid rejectedByUserId, DateTimeOffset rejectedAt, string reason)
    {
        if (Status != PettyCashCashCountStatus.Submitted)
        {
            throw new DomainValidationException("Only a submitted cash count can be rejected.");
        }

        RejectedByUserId = rejectedByUserId == Guid.Empty
            ? throw new DomainValidationException("The cash-count reviewer is required.")
            : rejectedByUserId;
        RejectedAt = rejectedAt;
        RejectionReason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 1000);
        Status = PettyCashCashCountStatus.Rejected;
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
        string? notes,
        Guid? pettyCashRequestLineId = null)
    {
        PettyCashFundId = pettyCashFundId;
        PettyCashRequestLineId = pettyCashRequestLineId;
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

    /// <summary>
    /// The funded category this movement belongs to, when it belongs to one. Null for fund-level
    /// movements such as opening balance, top-ups and adjustments, which are not tied to a request.
    /// </summary>
    public Guid? PettyCashRequestLineId { get; private set; }

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
    Cancelled = 6,

    /// <summary>
    /// Head office has checked the returned cash, the bills and any out-of-pocket balance the
    /// custodian recorded at settlement. Settled means the custodian says it adds up; this means
    /// head office agrees, and is the point after which the IOU is closed for good.
    /// </summary>
    SettlementApproved = 7,

    /// <summary>The receiver has assigned the request to a named operational approver.</summary>
    AwaitingAssignedApproval = 8,

    /// <summary>The assigned approver has checked/edited it and returned it to the receiver.</summary>
    ReturnedToReviewer = 9,

    /// <summary>The receiver has forwarded the reviewed request to head office.</summary>
    AwaitingHeadOfficeApproval = 10
}

public sealed class PettyCashIou : AuditableEntity
{
    private PettyCashIou() { }

    public PettyCashIou(
        string number,
        Guid? serviceJobId,
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

    /// <summary>
    /// For an advance handed over on a pre-printed slip this is the number printed on that slip -
    /// the paper is the document, and inventing a second number for it would leave two identities
    /// for one thing. Advances raised as a request in the system keep a generated number, because
    /// no slip exists yet when they are created.
    /// </summary>
    public string Number { get; private set; } = null!;

    /// <summary>Null when the cash was not drawn against a job - the slip records that as
    /// Location/Dept rather than a job order.</summary>
    public Guid? ServiceJobId { get; private set; }

    public Guid? ServiceJobDailySheetId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    /// <summary>
    /// The employee who physically collected the cash. This is deliberately separate from the
    /// requester: a supervisor may raise the job request and send another employee to collect it.
    /// </summary>
    public Guid? IssuedToUserId { get; private set; }
    public string? IssuedToName { get; private set; }
    public decimal Amount { get; private set; }
    public decimal ReleasedAmount { get; private set; }
    public string Purpose { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ExpectedSettlementAt { get; private set; }
    public PettyCashIouStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public Guid? ReviewerUserId { get; private set; }
    public string? ReviewerName { get; private set; }
    public Guid? AssignedApproverUserId { get; private set; }
    public string? AssignedApproverName { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? AssignedApprovedAt { get; private set; }
    public DateTimeOffset? HeadOfficeSubmittedAt { get; private set; }
    public Guid? HeadOfficeSubmittedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? PettyCashFundId { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public string? ReleaseReference { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public decimal? SettledAmount { get; private set; }
    public string? SettlementReference { get; private set; }

    /// <summary>
    /// The number of the paper bill the holder signed when the cash was handed over. Captured at
    /// release because that is the moment the signature happens, and it is the custodian's only
    /// proof for cash issued verbally rather than against a written request.
    /// </summary>
    public string? IssueBillNumber { get; private set; }

    /// <summary>The funded category the cash came out of, so releasing draws down that sub-account.</summary>
    public Guid? PettyCashRequestLineId { get; private set; }

    /// <summary>Cash handed back so far, across however many instalments it came in.</summary>
    public decimal ReturnedAmount { get; private set; }

    public DateTimeOffset? LastReturnedAt { get; private set; }

    public DateTimeOffset? SettlementApprovedAt { get; private set; }
    public Guid? SettlementApprovedByUserId { get; private set; }
    public decimal SettlementExceptionAmount { get; private set; }
    public string? SettlementExceptionReason { get; private set; }
    public DateTimeOffset? SettlementExceptionApprovedAt { get; private set; }
    public Guid? SettlementExceptionApprovedByUserId { get; private set; }
    public Guid? SettlementExceptionExpenseClaimId { get; private set; }

    /// <summary>
    /// The advance, less what has come back. What is left has to be covered by bills; anything not
    /// covered is cash the holder cannot account for.
    /// </summary>
    public decimal OutstandingAmount => ReleasedAmount - ReturnedAmount;

    public decimal RemainingReleaseAmount => Math.Max(0m, Amount - ReleasedAmount);

    /// <summary>Additions are allowed until head office signs the settlement off.</summary>
    public bool IsOpenForAccounting => Status is PettyCashIouStatus.Released or PettyCashIouStatus.Settled;

    /// <summary>
    /// Cash handed over on a pre-printed slip, with no request behind it. The IOU is created
    /// already released, because the money has physically gone and walking it back through the
    /// request states would be a fiction. The slip number is the document number: the paper is the
    /// original, and the system is recording it rather than issuing its own.
    /// </summary>
    public static PettyCashIou IssueDirectly(
        string slipNumber,
        Guid? serviceJobId,
        Guid requestedByUserId,
        string requestedByName,
        decimal amount,
        string purpose,
        DateTimeOffset issuedAt,
        Guid pettyCashFundId,
        Guid? pettyCashRequestLineId,
        Guid? serviceJobDailySheetId = null)
    {
        var issueBillNumber = Guard.NotNullOrWhiteSpace(slipNumber, nameof(slipNumber), maxLength: 32);

        var iou = new PettyCashIou(
            issueBillNumber,
            serviceJobId,
            requestedByUserId,
            requestedByName,
            amount,
            purpose,
            issuedAt,
            expectedSettlementAt: null,
            serviceJobDailySheetId)
        {
            Status = PettyCashIouStatus.Released,
            ReleasedAmount = amount,
            SubmittedAt = issuedAt,
            ApprovedAt = issuedAt,
            ApprovedByUserId = requestedByUserId,
            PettyCashFundId = pettyCashFundId,
            ReleasedAt = issuedAt,
            PettyCashRequestLineId = pettyCashRequestLineId,
            IssueBillNumber = issueBillNumber,
            IssuedToUserId = requestedByUserId,
            IssuedToName = requestedByName,
        };

        return iou;
    }

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != PettyCashIouStatus.Draft)
        {
            throw new DomainValidationException("Only draft IOUs can be submitted.");
        }

        Status = PettyCashIouStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void UpdateBeforeApproval(
        Guid serviceJobId,
        decimal amount,
        string purpose,
        DateTimeOffset? expectedSettlementAt)
    {
        if (Status is not (PettyCashIouStatus.Draft
            or PettyCashIouStatus.Submitted
            or PettyCashIouStatus.AwaitingAssignedApproval))
        {
            throw new DomainValidationException("Only draft, submitted or assigned IOUs can be edited before head-office approval.");
        }

        if (serviceJobId == Guid.Empty)
        {
            throw new DomainValidationException("Service job is required.");
        }

        ServiceJobId = serviceJobId;
        Amount = Guard.Positive(amount, nameof(amount));
        Purpose = Guard.NotNullOrWhiteSpace(purpose, nameof(purpose), maxLength: 1000);
        ExpectedSettlementAt = expectedSettlementAt;
    }

    public void SelectFundForApprovalBatch(Guid pettyCashFundId)
    {
        if (Status != PettyCashIouStatus.Submitted)
        {
            throw new DomainValidationException("Only a submitted IOU can be added to an approval batch.");
        }

        PettyCashFundId = pettyCashFundId == Guid.Empty
            ? throw new DomainValidationException("Select a petty cash fund.")
            : pettyCashFundId;
    }

    public void SetAssignedApprovedAmount(decimal amount)
    {
        if (Status != PettyCashIouStatus.AwaitingAssignedApproval)
        {
            throw new DomainValidationException("Only the assigned approver can change the amount at this stage.");
        }

        Amount = Guard.Positive(amount, nameof(amount));
    }

    public void AssignForApproval(
        Guid reviewerUserId,
        string reviewerName,
        Guid assignedApproverUserId,
        string assignedApproverName,
        DateTimeOffset assignedAt)
    {
        if (Status != PettyCashIouStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted IOUs can be assigned for operational approval.");
        }

        if (reviewerUserId == Guid.Empty || assignedApproverUserId == Guid.Empty)
        {
            throw new DomainValidationException("The receiver and assigned approver are required.");
        }

        if (assignedApproverUserId == reviewerUserId || assignedApproverUserId == RequestedByUserId)
        {
            throw new DomainValidationException("The assigned approver must be different from the requester and receiver.");
        }

        ReviewerUserId = reviewerUserId;
        ReviewerName = Guard.NotNullOrWhiteSpace(reviewerName, nameof(reviewerName), maxLength: 256);
        AssignedApproverUserId = assignedApproverUserId;
        AssignedApproverName = Guard.NotNullOrWhiteSpace(assignedApproverName, nameof(assignedApproverName), maxLength: 256);
        AssignedAt = assignedAt;
        AssignedApprovedAt = null;
        HeadOfficeSubmittedAt = null;
        HeadOfficeSubmittedByUserId = null;
        Status = PettyCashIouStatus.AwaitingAssignedApproval;
    }

    public void ApproveAssigned(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashIouStatus.AwaitingAssignedApproval)
        {
            throw new DomainValidationException("Only an IOU awaiting assigned approval can be approved at this stage.");
        }

        if (approvedByUserId != AssignedApproverUserId)
        {
            throw new DomainValidationException("Only the assigned approver can approve this IOU.");
        }

        AssignedApprovedAt = approvedAt;
        Status = PettyCashIouStatus.ReturnedToReviewer;
    }

    public void SubmitToHeadOffice(Guid reviewerUserId, DateTimeOffset submittedAt)
    {
        if (Status != PettyCashIouStatus.ReturnedToReviewer)
        {
            throw new DomainValidationException("Only an IOU returned by its assigned approver can be submitted to head office.");
        }

        if (reviewerUserId != ReviewerUserId)
        {
            throw new DomainValidationException("Only the receiver who assigned this IOU can submit it to head office.");
        }

        HeadOfficeSubmittedByUserId = reviewerUserId;
        HeadOfficeSubmittedAt = submittedAt;
        Status = PettyCashIouStatus.AwaitingHeadOfficeApproval;
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashIouStatus.AwaitingHeadOfficeApproval)
        {
            throw new DomainValidationException("Only IOUs submitted to head office can receive final approval.");
        }

        Status = PettyCashIouStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? rejectionReason)
    {
        if (Status is not (PettyCashIouStatus.Submitted
            or PettyCashIouStatus.AwaitingAssignedApproval
            or PettyCashIouStatus.AwaitingHeadOfficeApproval))
        {
            throw new DomainValidationException("Only IOUs in an approval stage can be rejected.");
        }

        Status = PettyCashIouStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
            ? null
            : Guard.NotNullOrWhiteSpace(rejectionReason, nameof(rejectionReason), maxLength: 512);
    }

    public void Release(
        Guid pettyCashFundId,
        decimal amount,
        DateTimeOffset releasedAt,
        string? releaseReference,
        string issueBillNumber,
        Guid issuedToUserId,
        string issuedToName,
        Guid? pettyCashRequestLineId = null)
    {
        if (Status is not (PettyCashIouStatus.Approved or PettyCashIouStatus.Released))
        {
            throw new DomainValidationException("Only approved or partially released IOUs can be released.");
        }

        var validatedAmount = Guard.Positive(amount, nameof(amount));
        if (validatedAmount > RemainingReleaseAmount)
        {
            throw new DomainValidationException(
                $"Release amount {validatedAmount:0.00} exceeds the {RemainingReleaseAmount:0.00} remaining on this IOU.");
        }

        if (PettyCashFundId is not null && PettyCashFundId != pettyCashFundId)
        {
            throw new DomainValidationException("This IOU was approved against a different petty cash fund.");
        }

        PettyCashFundId = pettyCashFundId;
        ReleasedAt = releasedAt;
        ReleaseReference = string.IsNullOrWhiteSpace(releaseReference) ? null : Guard.NotNullOrWhiteSpace(releaseReference, nameof(releaseReference), maxLength: 128);
        IssueBillNumber = Guard.NotNullOrWhiteSpace(issueBillNumber, nameof(issueBillNumber), maxLength: 64);
        IssuedToUserId = issuedToUserId == Guid.Empty
            ? throw new DomainValidationException("The employee collecting the cash is required.")
            : issuedToUserId;
        IssuedToName = Guard.NotNullOrWhiteSpace(issuedToName, nameof(issuedToName), maxLength: 256);
        PettyCashRequestLineId = pettyCashRequestLineId;
        ReleasedAmount += validatedAmount;
        Status = PettyCashIouStatus.Released;
    }

    /// <summary>
    /// Cash handed back. Holders often return it in instalments, so this accumulates rather than
    /// replacing, and stays open while the advance is Released or Settled - head office approval is
    /// what closes the advance, not the first return.
    /// </summary>
    public void AddReturn(decimal amount, DateTimeOffset returnedAt)
    {
        EnsureOpenForAccounting();
        var validatedAmount = Guard.Positive(amount, nameof(amount));
        if (validatedAmount > OutstandingAmount)
        {
            throw new DomainValidationException(
                $"Returning {validatedAmount:0.00} is more than the {OutstandingAmount:0.00} still outstanding on this advance.");
        }

        ReturnedAmount += validatedAmount;
        LastReturnedAt = returnedAt;

        // Returns are deliberately allowed until head office approves the settlement. Keep the
        // stored spent amount in sync when cash arrives after the custodian first settled the IOU.
        if (Status == PettyCashIouStatus.Settled)
        {
            SettledAmount = ReleasedAmount - ReturnedAmount;
        }
    }

    /// <summary>
    /// Marks the advance as accounted for. There is no amount to type: what was spent is simply the
    /// advance less what came back, and the bills behind it are the vouchers linked to this record.
    /// </summary>
    public void Settle(DateTimeOffset settledAt, string? settlementReference)
    {
        EnsureOpenForAccounting();

        if (RemainingReleaseAmount > 0m)
        {
            throw new DomainValidationException(
                $"The IOU still has {RemainingReleaseAmount:0.00} approved but unreleased. Release it before settlement.");
        }

        SettledAmount = ReleasedAmount - ReturnedAmount;
        SettledAt = settledAt;
        SettlementReference = string.IsNullOrWhiteSpace(settlementReference) ? null : Guard.NotNullOrWhiteSpace(settlementReference, nameof(settlementReference), maxLength: 128);
        Status = PettyCashIouStatus.Settled;
    }

    /// <summary>
    /// Authorizes a documented shortage or missing-receipt amount. The application service derives
    /// the amount from released cash, returns, and accepted bills; users cannot type an arbitrary
    /// write-off. Supporting evidence remains attached to the IOU collaboration record.
    /// </summary>
    public void ApproveSettlementException(
        decimal amount,
        string reason,
        Guid approvedByUserId,
        DateTimeOffset approvedAt,
        Guid? settlementExceptionExpenseClaimId = null)
    {
        EnsureOpenForAccounting();
        SettlementExceptionAmount = Guard.Positive(amount, nameof(amount));
        SettlementExceptionReason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 1000);
        SettlementExceptionApprovedByUserId = approvedByUserId == Guid.Empty
            ? throw new DomainValidationException("The exception approver is required.")
            : approvedByUserId;
        SettlementExceptionApprovedAt = approvedAt;
        SettlementExceptionExpenseClaimId = settlementExceptionExpenseClaimId;
    }

    private void EnsureOpenForAccounting()
    {
        if (Status is not (PettyCashIouStatus.Released or PettyCashIouStatus.Settled))
        {
            throw new DomainValidationException(
                "Only a released advance can be accounted for, and only until head office approves the settlement.");
        }
    }

    public void ApproveSettlement(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashIouStatus.Settled)
        {
            throw new DomainValidationException("Only a settled IOU can have its settlement approved.");
        }

        Status = PettyCashIouStatus.SettlementApproved;
        SettlementApprovedAt = approvedAt;
        SettlementApprovedByUserId = approvedByUserId;
    }

    public void Cancel()
    {
        if (Status is PettyCashIouStatus.Released or PettyCashIouStatus.Settled or PettyCashIouStatus.SettlementApproved)
        {
            throw new DomainValidationException("Released or settled IOUs cannot be cancelled.");
        }

        Status = PettyCashIouStatus.Cancelled;
    }
}

using ISS.Domain.Common;
using ISS.Domain.Finance;

namespace ISS.Domain.Service;

public enum ServiceExpenseFundingSource
{
    OutOfPocket = 1,
    PettyCash = 2
}

public enum ServiceExpenseClaimStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Settled = 4
}

public sealed class ServiceExpenseClaim : AuditableEntity
{
    private ServiceExpenseClaim() { }

    public ServiceExpenseClaim(
        string number,
        Guid serviceJobId,
        Guid? claimedByUserId,
        string claimedByName,
        ServiceExpenseFundingSource fundingSource,
        DateTimeOffset expenseDate,
        string? merchantName,
        string? receiptReference,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        ServiceJobId = serviceJobId;
        ClaimedByUserId = claimedByUserId;
        ClaimedByName = Guard.NotNullOrWhiteSpace(claimedByName, nameof(claimedByName), maxLength: 256);
        FundingSource = fundingSource;
        ExpenseDate = expenseDate;
        MerchantName = string.IsNullOrWhiteSpace(merchantName)
            ? null
            : Guard.NotNullOrWhiteSpace(merchantName, nameof(merchantName), maxLength: 256);
        ReceiptReference = string.IsNullOrWhiteSpace(receiptReference)
            ? null
            : Guard.NotNullOrWhiteSpace(receiptReference, nameof(receiptReference), maxLength: 128);
        Notes = notes?.Trim();
        Status = ServiceExpenseClaimStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public Guid? ClaimedByUserId { get; private set; }
    public string ClaimedByName { get; private set; } = null!;
    public ServiceExpenseFundingSource FundingSource { get; private set; }
    public DateTimeOffset ExpenseDate { get; private set; }
    public string? MerchantName { get; private set; }
    public string? ReceiptReference { get; private set; }
    public string? Notes { get; private set; }
    public ServiceExpenseClaimStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? SettlementPaymentTypeId { get; private set; }
    public Guid? SettlementPettyCashFundId { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public string? SettlementReference { get; private set; }

    public List<ServiceExpenseClaimLine> Lines { get; private set; } = new();

    public ServiceExpenseClaimLine AddLine(
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        Guid? expenseAccountId = null)
    {
        EnsureDraftEditable();

        var line = new ServiceExpenseClaimLine(
            Id,
            itemId,
            Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512),
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitCost, nameof(unitCost)),
            billableToCustomer,
            expenseAccountId);

        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        Guid? expenseAccountId = null)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service expense claim line not found.");

        line.Update(itemId, description, quantity, unitCost, billableToCustomer, expenseAccountId);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service expense claim line not found.");

        Lines.Remove(line);
    }

    public decimal Total => Lines.Sum(x => x.LineTotal);

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != ServiceExpenseClaimStatus.Draft)
        {
            throw new DomainValidationException("Only draft expense claims can be submitted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Expense claim must have at least one line.");
        }

        Status = ServiceExpenseClaimStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        if (Status != ServiceExpenseClaimStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted expense claims can be approved.");
        }

        Status = ServiceExpenseClaimStatus.Approved;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? rejectionReason)
    {
        if (Status != ServiceExpenseClaimStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted expense claims can be rejected.");
        }

        Status = ServiceExpenseClaimStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = rejectionReason?.Trim();
    }

    public void Settle(
        DateTimeOffset settledAt,
        Guid? settlementPaymentTypeId,
        Guid? settlementPettyCashFundId,
        string? settlementReference)
    {
        if (Status != ServiceExpenseClaimStatus.Approved)
        {
            throw new DomainValidationException("Only approved expense claims can be settled.");
        }

        if (FundingSource == ServiceExpenseFundingSource.PettyCash && settlementPettyCashFundId is null)
        {
            throw new DomainValidationException("Petty cash claims must be settled against a petty cash fund.");
        }

        Status = ServiceExpenseClaimStatus.Settled;
        SettledAt = settledAt;
        SettlementPaymentTypeId = settlementPaymentTypeId;
        SettlementPettyCashFundId = settlementPettyCashFundId;
        SettlementReference = settlementReference?.Trim();
    }

    private void EnsureDraftEditable()
    {
        if (Status != ServiceExpenseClaimStatus.Draft)
        {
            throw new DomainValidationException("Only draft expense claims can be edited.");
        }
    }
}

public sealed class ServiceExpenseClaimLine : Entity
{
    private ServiceExpenseClaimLine() { }

    public ServiceExpenseClaimLine(
        Guid serviceExpenseClaimId,
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        Guid? expenseAccountId = null)
    {
        ServiceExpenseClaimId = serviceExpenseClaimId;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitCost = unitCost;
        BillableToCustomer = billableToCustomer;
        ExpenseAccountId = expenseAccountId;
    }

    public Guid ServiceExpenseClaimId { get; private set; }
    public Guid? ItemId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public bool BillableToCustomer { get; private set; }
    public Guid? ExpenseAccountId { get; private set; }
    public LedgerAccount? ExpenseAccount { get; private set; }
    public Guid? ConvertedToServiceEstimateId { get; private set; }
    public Guid? ConvertedToServiceEstimateLineId { get; private set; }
    public DateTimeOffset? ConvertedToEstimateAt { get; private set; }
    public decimal LineTotal => Quantity * UnitCost;

    public void Update(
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        Guid? expenseAccountId = null)
    {
        ItemId = itemId;
        Description = Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512);
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
        BillableToCustomer = billableToCustomer;
        ExpenseAccountId = expenseAccountId;
    }

    public void AssignExpenseAccount(Guid? expenseAccountId) => ExpenseAccountId = expenseAccountId;

    public void MarkConvertedToEstimate(Guid serviceEstimateId, Guid serviceEstimateLineId, DateTimeOffset convertedAt)
    {
        if (!BillableToCustomer)
        {
            throw new DomainValidationException("Only billable expense claim lines can be converted to a service estimate.");
        }

        if (ConvertedToServiceEstimateLineId is not null)
        {
            throw new DomainValidationException("Expense claim line has already been converted to a service estimate.");
        }

        ConvertedToServiceEstimateId = serviceEstimateId;
        ConvertedToServiceEstimateLineId = serviceEstimateLineId;
        ConvertedToEstimateAt = convertedAt;
    }
}

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
        Guid? serviceJobId,
        Guid? claimedByUserId,
        string claimedByName,
        ServiceExpenseFundingSource fundingSource,
        DateTimeOffset expenseDate,
        string? merchantName,
        string? receiptReference,
        string? notes,
        Guid? serviceJobDailySheetId = null,
        Guid? pettyCashIouId = null,
        Guid? pettyCashRequestLineId = null,
        string? costCenterCode = null)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        ServiceJobId = serviceJobId;
        ServiceJobDailySheetId = serviceJobDailySheetId;
        PettyCashIouId = pettyCashIouId;
        PettyCashRequestLineId = pettyCashRequestLineId;
        CostCenterCode = string.IsNullOrWhiteSpace(costCenterCode)
            ? null
            : Guard.NotNullOrWhiteSpace(costCenterCode, nameof(costCenterCode), maxLength: 64).ToUpperInvariant();
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

    /// <summary>
    /// Cash paid straight out of the float for something bought on the spot - a taxi, a courier -
    /// where no one is left accountable and the bill is the whole of the support. The voucher is
    /// created already settled because the money has physically gone; the authority for it was the
    /// funded category, approved when head office released it. Contrast an advance, which leaves a
    /// person owing until they settle.
    /// </summary>
    public static ServiceExpenseClaim PayDirectlyFromFund(
        string number,
        Guid? serviceJobId,
        Guid? paidByUserId,
        string paidByName,
        DateTimeOffset paidAt,
        string description,
        decimal amount,
        bool billableToCustomer,
        string? merchantName,
        string receiptReference,
        string? notes,
        Guid pettyCashFundId,
        Guid? pettyCashRequestLineId,
        string? costCenterCode = null,
        Guid? expenseAccountId = null)
    {
        var claim = new ServiceExpenseClaim(
            number,
            serviceJobId,
            paidByUserId,
            paidByName,
            ServiceExpenseFundingSource.PettyCash,
            paidAt,
            merchantName,
            Guard.NotNullOrWhiteSpace(receiptReference, nameof(receiptReference), maxLength: 128),
            notes,
            serviceJobDailySheetId: null,
            pettyCashIouId: null,
            pettyCashRequestLineId,
            costCenterCode);

        // Added while still Draft, since AddLine refuses anything else.
        claim.AddLine(
            null,
            description,
            1m,
            Guard.Positive(amount, nameof(amount)),
            billableToCustomer,
            expenseAccountId,
            receiptReference);

        claim.Status = ServiceExpenseClaimStatus.Settled;
        claim.SubmittedAt = paidAt;
        claim.ApprovedAt = paidAt;
        claim.SettledAt = paidAt;
        claim.SettlementPettyCashFundId = pettyCashFundId;
        claim.SettlementReference = receiptReference;

        return claim;
    }

    public string Number { get; private set; } = null!;

    /// <summary>
    /// The job this spend belongs to, when it belongs to one. Null for overhead the custodian pays
    /// under their own name - transportation, emergency callouts - which is real spend but is not
    /// attributable to any single job and so never enters job costing.
    /// </summary>
    public Guid? ServiceJobId { get; private set; }

    public Guid? ServiceJobDailySheetId { get; private set; }

    /// <summary>
    /// The petty cash advance this spend was made from, when there was one. Settling an IOU records
    /// only how much of the advance was spent; without this link nothing says what it was spent on,
    /// so an advance can be settled at 600 against 100 of documented claims and no screen notices.
    /// Null for out-of-pocket claims and for petty cash spent outside an advance.
    /// </summary>
    public Guid? PettyCashIouId { get; private set; }

    /// <summary>
    /// The funded category this spend is charged against, so settling it draws down that
    /// sub-account rather than the float as an undifferentiated whole.
    /// </summary>
    public Guid? PettyCashRequestLineId { get; private set; }
    public string? CostCenterCode { get; private set; }
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
        Guid? expenseAccountId = null,
        string? receiptReference = null,
        bool missingReceipt = false,
        string? missingReceiptReason = null)
    {
        EnsureDraftEditable();

        var line = new ServiceExpenseClaimLine(
            Id,
            itemId,
            Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512),
            Guard.Positive(quantity, nameof(quantity)),
            Guard.NotNegative(unitCost, nameof(unitCost)),
            billableToCustomer,
            expenseAccountId,
            receiptReference,
            missingReceipt,
            missingReceiptReason);

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
        Guid? expenseAccountId = null,
        string? receiptReference = null,
        bool missingReceipt = false,
        string? missingReceiptReason = null)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service expense claim line not found.");

        line.Update(
            itemId,
            description,
            quantity,
            unitCost,
            billableToCustomer,
            expenseAccountId,
            receiptReference,
            missingReceipt,
            missingReceiptReason);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();

        var line = Lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainValidationException("Service expense claim line not found.");

        Lines.Remove(line);
    }

    public decimal Total => Lines.Sum(x => x.LineTotal);

    /// <summary>
    /// Points this claim at the advance that funded it, or clears the link. Draft only — once the
    /// claim is submitted the reconciliation figures on the IOU are being read by approvers, and
    /// moving a claim between advances underneath them would silently change both.
    /// </summary>
    public void LinkPettyCashIou(Guid? pettyCashIouId)
    {
        EnsureDraftEditable();

        if (pettyCashIouId is not null && FundingSource != ServiceExpenseFundingSource.PettyCash)
        {
            throw new DomainValidationException("Only petty cash claims can be linked to an IOU advance.");
        }

        PettyCashIouId = pettyCashIouId;
    }

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

        if (Lines.Any(line => line.ExpenseAccountId is null))
        {
            throw new DomainValidationException("Every expense line requires an expense category/account before submission.");
        }

        if (FundingSource == ServiceExpenseFundingSource.PettyCash
            && Lines.Any(line => !line.MissingReceipt && string.IsNullOrWhiteSpace(line.ReceiptReference)))
        {
            throw new DomainValidationException(
                "Every petty-cash expense line requires its own receipt reference.");
        }

        if (FundingSource == ServiceExpenseFundingSource.PettyCash
            && Lines.Any(line => line.MissingReceipt && line.MissingReceiptApprovedAt is null))
        {
            throw new DomainValidationException(
                "Every missing-receipt expense line requires higher-level approval before submission.");
        }

        if (FundingSource == ServiceExpenseFundingSource.PettyCash
            && ServiceJobId is null
            && string.IsNullOrWhiteSpace(CostCenterCode))
        {
            throw new DomainValidationException("A cost centre is required for non-job petty cash expenditure.");
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
        Guid? expenseAccountId = null,
        string? receiptReference = null,
        bool missingReceipt = false,
        string? missingReceiptReason = null)
    {
        ServiceExpenseClaimId = serviceExpenseClaimId;
        ItemId = itemId;
        Description = description;
        Quantity = quantity;
        UnitCost = unitCost;
        BillableToCustomer = billableToCustomer;
        ExpenseAccountId = expenseAccountId;
        SetReceiptEvidence(receiptReference, missingReceipt, missingReceiptReason);
    }

    public Guid ServiceExpenseClaimId { get; private set; }
    public Guid? ItemId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public bool BillableToCustomer { get; private set; }
    public Guid? ExpenseAccountId { get; private set; }
    public LedgerAccount? ExpenseAccount { get; private set; }
    public string? ReceiptReference { get; private set; }
    public bool MissingReceipt { get; private set; }
    public string? MissingReceiptReason { get; private set; }
    public DateTimeOffset? MissingReceiptApprovedAt { get; private set; }
    public Guid? MissingReceiptApprovedByUserId { get; private set; }
    public Guid? ConvertedToServiceEstimateId { get; private set; }
    public Guid? ConvertedToServiceEstimateLineId { get; private set; }
    public DateTimeOffset? ConvertedToEstimateAt { get; private set; }

    /// <summary>
    /// The invoice line that charged this expense to the customer. Mirrors the labour entry link,
    /// so the billing screen can tell what has already been recovered and never offer it twice.
    /// </summary>
    public Guid? SalesInvoiceId { get; private set; }
    public Guid? SalesInvoiceLineId { get; private set; }
    public DateTimeOffset? InvoicedAt { get; private set; }

    public decimal LineTotal => Quantity * UnitCost;

    public void MarkInvoiced(Guid salesInvoiceId, Guid salesInvoiceLineId, DateTimeOffset invoicedAt)
    {
        if (!BillableToCustomer)
        {
            throw new DomainValidationException("Only billable expense lines can be invoiced.");
        }

        SalesInvoiceId = salesInvoiceId;
        SalesInvoiceLineId = salesInvoiceLineId;
        InvoicedAt = invoicedAt;
    }

    public void Update(
        Guid? itemId,
        string description,
        decimal quantity,
        decimal unitCost,
        bool billableToCustomer,
        Guid? expenseAccountId = null,
        string? receiptReference = null,
        bool missingReceipt = false,
        string? missingReceiptReason = null)
    {
        ItemId = itemId;
        Description = Guard.NotNullOrWhiteSpace(description, nameof(description), maxLength: 512);
        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitCost = Guard.NotNegative(unitCost, nameof(unitCost));
        BillableToCustomer = billableToCustomer;
        ExpenseAccountId = expenseAccountId;
        SetReceiptEvidence(receiptReference, missingReceipt, missingReceiptReason);
    }

    public void ApproveMissingReceipt(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (!MissingReceipt)
        {
            throw new DomainValidationException("This expense line is not requesting a missing-receipt exception.");
        }

        MissingReceiptApprovedByUserId = approvedByUserId == Guid.Empty
            ? throw new DomainValidationException("The missing-receipt approver is required.")
            : approvedByUserId;
        MissingReceiptApprovedAt = approvedAt;
    }

    private void SetReceiptEvidence(string? receiptReference, bool missingReceipt, string? missingReceiptReason)
    {
        ReceiptReference = string.IsNullOrWhiteSpace(receiptReference)
            ? null
            : Guard.NotNullOrWhiteSpace(receiptReference, nameof(receiptReference), maxLength: 128);
        MissingReceipt = missingReceipt;
        MissingReceiptReason = missingReceipt
            ? Guard.NotNullOrWhiteSpace(missingReceiptReason, nameof(missingReceiptReason), maxLength: 1000)
            : null;
        // Editing evidence invalidates any previous exception approval.
        MissingReceiptApprovedAt = null;
        MissingReceiptApprovedByUserId = null;

        if (missingReceipt)
        {
            ReceiptReference = null;
        }
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

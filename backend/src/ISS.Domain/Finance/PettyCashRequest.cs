using ISS.Domain.Common;

namespace ISS.Domain.Finance;

/// <summary>
/// What the money was asked for. Head office funds each category separately even when one bank
/// transfer covers several, so the category lives on the line rather than on the request.
/// </summary>
public enum PettyCashRequestCategory
{
    JobWise = 1,
    EmergencyOperation = 2,
    Transportation = 3,
    Custom = 4
}

public enum PettyCashRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    PartiallyFunded = 3,
    Funded = 4,
    Rejected = 5,
    Cancelled = 6
}

/// <summary>
/// A site accountant's request to head office for petty cash, broken into category lines.
///
/// This is the stage before an advance: it puts money into the custodian's float. The IOU that
/// later hands cash to a worker draws that float down. Both post to the same
/// <see cref="PettyCashFund"/> ledger, so the fund balance stays the single source of truth and
/// each category's sub-balance is that ledger filtered by request line.
/// </summary>
public sealed class PettyCashRequest : AuditableEntity
{
    private PettyCashRequest() { }

    public PettyCashRequest(
        string number,
        Guid pettyCashFundId,
        Guid requestedByUserId,
        string requestedByName,
        DateTimeOffset requestedAt,
        DateTimeOffset? neededByAt,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        PettyCashFundId = pettyCashFundId;
        RequestedByUserId = requestedByUserId;
        RequestedByName = Guard.NotNullOrWhiteSpace(requestedByName, nameof(requestedByName), maxLength: 256);
        RequestedAt = requestedAt;
        NeededByAt = neededByAt;
        Notes = NormalizeOptional(notes, nameof(notes), 1000);
        Status = PettyCashRequestStatus.Draft;
    }

    public string Number { get; private set; } = null!;

    /// <summary>The float the approved money is paid into - the custodian's own account.</summary>
    public Guid PettyCashFundId { get; private set; }

    public Guid RequestedByUserId { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? NeededByAt { get; private set; }
    public string? Notes { get; private set; }
    public PettyCashRequestStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public List<PettyCashRequestLine> Lines { get; private set; } = new();

    public decimal RequestedTotal => Lines.Sum(x => x.RequestedAmount);
    public decimal ApprovedTotal => Lines.Sum(x => x.ApprovedAmount ?? 0m);
    public decimal FundedTotal => Lines.Sum(x => x.FundedAmount);
    public decimal OutstandingTotal => Lines.Sum(x => x.OutstandingAmount);

    public PettyCashRequestLine AddLine(
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount)
    {
        EnsureDraftEditable();

        var line = new PettyCashRequestLine(
            Id,
            category,
            serviceJobId,
            customCategoryName,
            purpose,
            requestedAmount);

        Lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount)
    {
        EnsureDraftEditable();
        FindLine(lineId).Update(category, serviceJobId, customCategoryName, purpose, requestedAmount);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraftEditable();
        Lines.Remove(FindLine(lineId));
    }

    public void UpdateHeader(DateTimeOffset? neededByAt, string? notes)
    {
        EnsureDraftEditable();
        NeededByAt = neededByAt;
        Notes = NormalizeOptional(notes, nameof(notes), 1000);
    }

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != PettyCashRequestStatus.Draft)
        {
            throw new DomainValidationException("Only draft petty cash requests can be submitted.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("A petty cash request must have at least one category line.");
        }

        Status = PettyCashRequestStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    /// <summary>
    /// Head office decides each line on its own: a line can be approved for less than was asked,
    /// or for nothing at all. Approving every line at zero is a rejection and is refused here so
    /// that it goes through <see cref="Reject"/> and carries a reason.
    /// </summary>
    public void Approve(
        Guid approvedByUserId,
        DateTimeOffset approvedAt,
        IReadOnlyDictionary<Guid, decimal> approvedAmountsByLineId)
    {
        if (Status != PettyCashRequestStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash requests can be approved.");
        }

        foreach (var line in Lines)
        {
            if (!approvedAmountsByLineId.TryGetValue(line.Id, out var approvedAmount))
            {
                throw new DomainValidationException($"Line '{line.Purpose}' has no approved amount.");
            }

            line.SetApprovedAmount(approvedAmount);
        }

        if (ApprovedTotal <= 0m)
        {
            throw new DomainValidationException("Approving every line at zero is a rejection - reject the request instead so it carries a reason.");
        }

        Status = PettyCashRequestStatus.Approved;
        ApprovedAt = approvedAt;
        ApprovedByUserId = approvedByUserId;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? rejectionReason)
    {
        if (Status != PettyCashRequestStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash requests can be rejected.");
        }

        Status = PettyCashRequestStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = NormalizeOptional(rejectionReason, nameof(rejectionReason), 512);
    }

    public void Cancel()
    {
        if (Status is PettyCashRequestStatus.PartiallyFunded or PettyCashRequestStatus.Funded)
        {
            throw new DomainValidationException("Money has already been released against this request, so it cannot be cancelled.");
        }

        if (Status is PettyCashRequestStatus.Rejected or PettyCashRequestStatus.Cancelled)
        {
            throw new DomainValidationException("This petty cash request is already closed.");
        }

        Status = PettyCashRequestStatus.Cancelled;
    }

    /// <summary>
    /// Records money actually paid against one line. Returns the funding so the caller can post the
    /// matching fund transaction; the two are written in the same save, and the ledger is what the
    /// balances are read from.
    /// </summary>
    public PettyCashRequestLineFunding RecordFunding(
        Guid lineId,
        decimal amount,
        DateTimeOffset fundedAt,
        string? paymentReference,
        string? notes)
    {
        if (Status is not (PettyCashRequestStatus.Approved or PettyCashRequestStatus.PartiallyFunded))
        {
            throw new DomainValidationException("Only an approved petty cash request can be funded.");
        }

        var funding = FindLine(lineId).RecordFunding(amount, fundedAt, paymentReference, notes);
        Status = OutstandingTotal <= 0m ? PettyCashRequestStatus.Funded : PettyCashRequestStatus.PartiallyFunded;
        return funding;
    }

    private PettyCashRequestLine FindLine(Guid lineId)
        => Lines.FirstOrDefault(x => x.Id == lineId)
           ?? throw new DomainValidationException("Petty cash request line not found.");

    private void EnsureDraftEditable()
    {
        if (Status != PettyCashRequestStatus.Draft)
        {
            throw new DomainValidationException("Only draft petty cash requests can be edited.");
        }
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.NotNullOrWhiteSpace(value, paramName, maxLength: maxLength);
}

public sealed class PettyCashRequestLine : Entity
{
    private PettyCashRequestLine() { }

    public PettyCashRequestLine(
        Guid pettyCashRequestId,
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount)
    {
        PettyCashRequestId = pettyCashRequestId;
        Apply(category, serviceJobId, customCategoryName, purpose, requestedAmount);
    }

    public Guid PettyCashRequestId { get; private set; }
    public PettyCashRequestCategory Category { get; private set; }

    /// <summary>Set only for <see cref="PettyCashRequestCategory.JobWise"/>; this is what makes the
    /// spend reachable by job costing once it is eventually claimed.</summary>
    public Guid? ServiceJobId { get; private set; }

    /// <summary>Set only for <see cref="PettyCashRequestCategory.Custom"/>.</summary>
    public string? CustomCategoryName { get; private set; }

    public string Purpose { get; private set; } = null!;
    public decimal RequestedAmount { get; private set; }
    public decimal? ApprovedAmount { get; private set; }

    public List<PettyCashRequestLineFunding> Fundings { get; private set; } = new();

    public decimal FundedAmount => Fundings.Sum(x => x.Amount);
    public decimal OutstandingAmount => Math.Max(0m, (ApprovedAmount ?? 0m) - FundedAmount);

    public void Update(
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount)
        => Apply(category, serviceJobId, customCategoryName, purpose, requestedAmount);

    public void SetApprovedAmount(decimal approvedAmount)
    {
        Guard.NotNegative(approvedAmount, nameof(approvedAmount));

        if (approvedAmount > RequestedAmount)
        {
            throw new DomainValidationException($"Approved amount cannot exceed the {RequestedAmount} requested on '{Purpose}'.");
        }

        if (approvedAmount < FundedAmount)
        {
            throw new DomainValidationException($"Approved amount cannot be less than the {FundedAmount} already funded on '{Purpose}'.");
        }

        ApprovedAmount = approvedAmount;
    }

    public PettyCashRequestLineFunding RecordFunding(
        decimal amount,
        DateTimeOffset fundedAt,
        string? paymentReference,
        string? notes)
    {
        Guard.Positive(amount, nameof(amount));

        if (amount > OutstandingAmount)
        {
            throw new DomainValidationException(
                $"Funding {amount} exceeds the {OutstandingAmount} still outstanding on '{Purpose}'.");
        }

        var funding = new PettyCashRequestLineFunding(Id, amount, fundedAt, paymentReference, notes);
        Fundings.Add(funding);
        return funding;
    }

    private void Apply(
        PettyCashRequestCategory category,
        Guid? serviceJobId,
        string? customCategoryName,
        string purpose,
        decimal requestedAmount)
    {
        Category = category;
        Purpose = Guard.NotNullOrWhiteSpace(purpose, nameof(purpose), maxLength: 512);
        RequestedAmount = Guard.Positive(requestedAmount, nameof(requestedAmount));

        // A job-wise line without a job cannot ever reach job costing, and a custom line without a
        // name is indistinguishable from the next one, so neither is allowed to be saved that way.
        if (category == PettyCashRequestCategory.JobWise)
        {
            ServiceJobId = serviceJobId
                           ?? throw new DomainValidationException("A job-wise petty cash line must name the job order it is for.");
        }
        else
        {
            ServiceJobId = null;
        }

        if (category == PettyCashRequestCategory.Custom)
        {
            CustomCategoryName = Guard.NotNullOrWhiteSpace(
                customCategoryName,
                nameof(customCategoryName),
                maxLength: 128);
        }
        else
        {
            CustomCategoryName = null;
        }
    }
}

public sealed class PettyCashRequestLineFunding : Entity
{
    private PettyCashRequestLineFunding() { }

    public PettyCashRequestLineFunding(
        Guid pettyCashRequestLineId,
        decimal amount,
        DateTimeOffset fundedAt,
        string? paymentReference,
        string? notes)
    {
        PettyCashRequestLineId = pettyCashRequestLineId;
        Amount = Guard.Positive(amount, nameof(amount));
        FundedAt = fundedAt;
        PaymentReference = string.IsNullOrWhiteSpace(paymentReference)
            ? null
            : Guard.NotNullOrWhiteSpace(paymentReference, nameof(paymentReference), maxLength: 128);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.NotNullOrWhiteSpace(notes, nameof(notes), maxLength: 512);
    }

    public Guid PettyCashRequestLineId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset FundedAt { get; private set; }

    /// <summary>The bank transfer or slip reference. Several lines funded by one transfer share
    /// this value, which is how a single payment covering many categories is represented.</summary>
    public string? PaymentReference { get; private set; }

    public string? Notes { get; private set; }
}

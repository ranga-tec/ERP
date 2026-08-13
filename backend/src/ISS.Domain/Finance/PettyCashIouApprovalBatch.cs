using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashIouApprovalBatchStatus
{
    AwaitingAssignedApproval = 1,
    ReturnedToReviewer = 2,
    AwaitingHeadOfficeApproval = 3,
    Approved = 4,
    FundingReceived = 5,
    Rejected = 6,
}

/// <summary>
/// An accountant's cover sheet for one or more submitted petty-cash advances. The individual IOUs
/// remain the employee-facing documents; this aggregate makes their operational approval,
/// head-office submission and fund receipt one auditable transaction.
/// </summary>
public sealed class PettyCashIouApprovalBatch : AuditableEntity
{
    private PettyCashIouApprovalBatch() { }

    public PettyCashIouApprovalBatch(
        string number,
        Guid pettyCashFundId,
        Guid reviewerUserId,
        string reviewerName,
        Guid assignedApproverUserId,
        string assignedApproverName,
        DateTimeOffset submittedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        PettyCashFundId = pettyCashFundId == Guid.Empty
            ? throw new DomainValidationException("Select a petty cash fund.")
            : pettyCashFundId;
        ReviewerUserId = reviewerUserId == Guid.Empty
            ? throw new DomainValidationException("The accountant receiving the requests is required.")
            : reviewerUserId;
        ReviewerName = Guard.NotNullOrWhiteSpace(reviewerName, nameof(reviewerName), maxLength: 256);
        AssignedApproverUserId = assignedApproverUserId == Guid.Empty
            ? throw new DomainValidationException("Select an approver.")
            : assignedApproverUserId;
        AssignedApproverName = Guard.NotNullOrWhiteSpace(assignedApproverName, nameof(assignedApproverName), maxLength: 256);
        if (ReviewerUserId == AssignedApproverUserId)
        {
            throw new DomainValidationException("The accountant and assigned approver must be different users.");
        }

        SubmittedAt = submittedAt;
        Status = PettyCashIouApprovalBatchStatus.AwaitingAssignedApproval;
    }

    public string Number { get; private set; } = null!;
    public Guid PettyCashFundId { get; private set; }
    public Guid ReviewerUserId { get; private set; }
    public string ReviewerName { get; private set; } = null!;
    public Guid AssignedApproverUserId { get; private set; }
    public string AssignedApproverName { get; private set; } = null!;
    public PettyCashIouApprovalBatchStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? AssignedApprovedAt { get; private set; }
    public DateTimeOffset? HeadOfficeSubmittedAt { get; private set; }
    public Guid? HeadOfficeSubmittedByUserId { get; private set; }
    public DateTimeOffset? HeadOfficeApprovedAt { get; private set; }
    public Guid? HeadOfficeApprovedByUserId { get; private set; }
    public DateTimeOffset? FundingReceivedAt { get; private set; }
    public Guid? FundingReceivedByUserId { get; private set; }
    public string? FundingReference { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public List<PettyCashIouApprovalBatchLine> Lines { get; private set; } = new();

    public decimal RequestedTotal => Lines.Sum(x => x.RequestedAmount);
    public decimal ApprovedTotal => Lines.Sum(x => x.ApprovedAmount);

    public PettyCashIouApprovalBatchLine AddLine(Guid pettyCashIouId, decimal requestedAmount)
    {
        if (Status != PettyCashIouApprovalBatchStatus.AwaitingAssignedApproval)
        {
            throw new DomainValidationException("Lines can only be added while the batch is being prepared.");
        }

        if (Lines.Any(x => x.PettyCashIouId == pettyCashIouId))
        {
            throw new DomainValidationException("The same IOU cannot appear twice in one approval batch.");
        }

        var line = new PettyCashIouApprovalBatchLine(Id, pettyCashIouId, requestedAmount);
        Lines.Add(line);
        return line;
    }

    public void ApproveAssigned(
        Guid approvedByUserId,
        DateTimeOffset approvedAt,
        IReadOnlyDictionary<Guid, decimal> approvedAmountsByLineId)
    {
        if (Status != PettyCashIouApprovalBatchStatus.AwaitingAssignedApproval)
        {
            throw new DomainValidationException("Only a batch awaiting assigned approval can be approved.");
        }

        if (approvedByUserId != AssignedApproverUserId)
        {
            throw new DomainValidationException("Only the assigned approver can approve this batch.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainValidationException("The approval batch has no IOUs.");
        }

        foreach (var line in Lines)
        {
            if (!approvedAmountsByLineId.TryGetValue(line.Id, out var amount))
            {
                throw new DomainValidationException("Every IOU must have an approved amount.");
            }

            line.SetApprovedAmount(amount);
        }

        if (ApprovedTotal <= 0m)
        {
            throw new DomainValidationException("A batch cannot be approved with every IOU amount set to zero. Reject it instead.");
        }

        AssignedApprovedAt = approvedAt;
        Status = PettyCashIouApprovalBatchStatus.ReturnedToReviewer;
    }

    public void SubmitToHeadOffice(Guid reviewerUserId, DateTimeOffset submittedAt)
    {
        if (Status != PettyCashIouApprovalBatchStatus.ReturnedToReviewer)
        {
            throw new DomainValidationException("Only a batch returned by its approver can be submitted to head office.");
        }

        if (reviewerUserId != ReviewerUserId)
        {
            throw new DomainValidationException("Only the accountant who prepared this batch can submit it to head office.");
        }

        HeadOfficeSubmittedAt = submittedAt;
        HeadOfficeSubmittedByUserId = reviewerUserId;
        Status = PettyCashIouApprovalBatchStatus.AwaitingHeadOfficeApproval;
    }

    public void ApproveHeadOffice(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashIouApprovalBatchStatus.AwaitingHeadOfficeApproval)
        {
            throw new DomainValidationException("Only a batch awaiting head-office approval can be approved.");
        }

        HeadOfficeApprovedAt = approvedAt;
        HeadOfficeApprovedByUserId = approvedByUserId;
        Status = PettyCashIouApprovalBatchStatus.Approved;
    }

    public void ReceiveFunding(Guid receivedByUserId, DateTimeOffset receivedAt, string fundingReference)
    {
        if (Status != PettyCashIouApprovalBatchStatus.Approved)
        {
            throw new DomainValidationException("Only a head-office-approved batch can be recorded as received.");
        }

        if (receivedByUserId != ReviewerUserId)
        {
            throw new DomainValidationException("Only the accountant who prepared this batch can record its funding receipt.");
        }

        FundingReference = Guard.NotNullOrWhiteSpace(fundingReference, nameof(fundingReference), maxLength: 128);
        FundingReceivedByUserId = receivedByUserId;
        FundingReceivedAt = receivedAt;
        Status = PettyCashIouApprovalBatchStatus.FundingReceived;
    }

    public void Reject(Guid rejectedByUserId, DateTimeOffset rejectedAt, string? reason)
    {
        if (Status is not (PettyCashIouApprovalBatchStatus.AwaitingAssignedApproval
            or PettyCashIouApprovalBatchStatus.AwaitingHeadOfficeApproval))
        {
            throw new DomainValidationException("Only a batch in an approval stage can be rejected.");
        }

        RejectedByUserId = rejectedByUserId;
        RejectedAt = rejectedAt;
        RejectionReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 512);
        Status = PettyCashIouApprovalBatchStatus.Rejected;
    }
}

public sealed class PettyCashIouApprovalBatchLine : Entity
{
    private PettyCashIouApprovalBatchLine() { }

    public PettyCashIouApprovalBatchLine(Guid batchId, Guid pettyCashIouId, decimal requestedAmount)
    {
        PettyCashIouApprovalBatchId = batchId;
        PettyCashIouId = pettyCashIouId;
        RequestedAmount = Guard.Positive(requestedAmount, nameof(requestedAmount));
        ApprovedAmount = RequestedAmount;
    }

    public Guid PettyCashIouApprovalBatchId { get; private set; }
    public Guid PettyCashIouId { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }

    public void SetApprovedAmount(decimal amount)
    {
        if (amount < 0m)
        {
            throw new DomainValidationException("Approved amount cannot be negative.");
        }

        var approved = amount;
        if (approved > RequestedAmount)
        {
            throw new DomainValidationException(
                $"Approved amount {approved:0.00} cannot exceed requested amount {RequestedAmount:0.00}.");
        }

        ApprovedAmount = approved;
    }
}

using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashReallocationStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// An approved change to the purpose for which cash already held in one petty-cash fund may be
/// used. Approval posts equal transfer-out and transfer-in ledger entries, so physical cash and the
/// fund total do not move while the two category sub-accounts retain a complete audit trail.
/// </summary>
public sealed class PettyCashReallocation : AuditableEntity
{
    private PettyCashReallocation() { }

    public PettyCashReallocation(
        string number,
        Guid pettyCashFundId,
        Guid sourcePettyCashRequestLineId,
        Guid destinationPettyCashRequestLineId,
        decimal amount,
        string reason,
        Guid requestedByUserId,
        string requestedByName,
        DateTimeOffset requestedAt)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        PettyCashFundId = pettyCashFundId == Guid.Empty
            ? throw new DomainValidationException("A petty cash fund is required.")
            : pettyCashFundId;
        SourcePettyCashRequestLineId = sourcePettyCashRequestLineId == Guid.Empty
            ? throw new DomainValidationException("A source funded category is required.")
            : sourcePettyCashRequestLineId;
        DestinationPettyCashRequestLineId = destinationPettyCashRequestLineId == Guid.Empty
            ? throw new DomainValidationException("A destination funded category is required.")
            : destinationPettyCashRequestLineId;
        if (SourcePettyCashRequestLineId == DestinationPettyCashRequestLineId)
        {
            throw new DomainValidationException("Source and destination categories must be different.");
        }

        Amount = Guard.Positive(amount, nameof(amount));
        Reason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 1000);
        RequestedByUserId = requestedByUserId == Guid.Empty
            ? throw new DomainValidationException("The person requesting the reallocation is required.")
            : requestedByUserId;
        RequestedByName = Guard.NotNullOrWhiteSpace(requestedByName, nameof(requestedByName), maxLength: 256);
        RequestedAt = requestedAt;
        Status = PettyCashReallocationStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid PettyCashFundId { get; private set; }
    public Guid SourcePettyCashRequestLineId { get; private set; }
    public Guid DestinationPettyCashRequestLineId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = null!;
    public Guid RequestedByUserId { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public PettyCashReallocationStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public void Submit(DateTimeOffset submittedAt)
    {
        EnsureDraft();
        Status = PettyCashReallocationStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        if (Status != PettyCashReallocationStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash reallocations can be approved.");
        }

        ApprovedByUserId = approvedByUserId == Guid.Empty
            ? throw new DomainValidationException("The head-office approver is required.")
            : approvedByUserId;
        ApprovedAt = approvedAt;
        Status = PettyCashReallocationStatus.Approved;
    }

    public void Reject(Guid rejectedByUserId, DateTimeOffset rejectedAt, string reason)
    {
        if (Status != PettyCashReallocationStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash reallocations can be rejected.");
        }

        RejectedByUserId = rejectedByUserId == Guid.Empty
            ? throw new DomainValidationException("The head-office reviewer is required.")
            : rejectedByUserId;
        RejectionReason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 512);
        RejectedAt = rejectedAt;
        Status = PettyCashReallocationStatus.Rejected;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        EnsureDraft();
        CancelledAt = cancelledAt;
        Status = PettyCashReallocationStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != PettyCashReallocationStatus.Draft)
        {
            throw new DomainValidationException("Only draft petty cash reallocations can be changed.");
        }
    }
}

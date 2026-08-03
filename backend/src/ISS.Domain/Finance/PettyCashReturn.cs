using ISS.Domain.Common;

namespace ISS.Domain.Finance;

public enum PettyCashReturnStatus
{
    Draft = 0,
    Submitted = 1,
    Received = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// A custody transfer that sends unused, reconciled petty cash back to head office. Lines retain
/// the original funding category so receipt can reverse the correct sub-account instead of using a
/// fund-level adjustment that loses the audit trail.
/// </summary>
public sealed class PettyCashReturn : AuditableEntity
{
    private PettyCashReturn() { }

    public PettyCashReturn(
        string number,
        Guid pettyCashFundId,
        Guid preparedByUserId,
        string preparedByName,
        DateTimeOffset preparedAt,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        PettyCashFundId = pettyCashFundId == Guid.Empty
            ? throw new DomainValidationException("A petty cash fund is required.")
            : pettyCashFundId;
        PreparedByUserId = preparedByUserId == Guid.Empty
            ? throw new DomainValidationException("The person preparing the return is required.")
            : preparedByUserId;
        PreparedByName = Guard.NotNullOrWhiteSpace(preparedByName, nameof(preparedByName), maxLength: 256);
        PreparedAt = preparedAt;
        Notes = NormalizeOptional(notes, nameof(notes), 1000);
        Status = PettyCashReturnStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid PettyCashFundId { get; private set; }
    public Guid PreparedByUserId { get; private set; }
    public string PreparedByName { get; private set; } = null!;
    public DateTimeOffset PreparedAt { get; private set; }
    public string? Notes { get; private set; }
    public PettyCashReturnStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public string? ReceiptReference { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public List<PettyCashReturnLine> Lines { get; private set; } = new();
    public decimal TotalAmount => Lines.Sum(x => x.Amount);

    public PettyCashReturnLine AddLine(Guid pettyCashRequestLineId, decimal amount)
    {
        EnsureDraft();
        if (Lines.Any(x => x.PettyCashRequestLineId == pettyCashRequestLineId))
        {
            throw new DomainValidationException("Each funded category can appear only once on a petty cash return.");
        }

        var line = new PettyCashReturnLine(Id, pettyCashRequestLineId, amount);
        Lines.Add(line);
        return line;
    }

    public void Submit(DateTimeOffset submittedAt)
    {
        EnsureDraft();
        if (Lines.Count == 0)
        {
            throw new DomainValidationException("Select at least one funded category to return.");
        }

        Status = PettyCashReturnStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void ConfirmReceived(Guid receivedByUserId, DateTimeOffset receivedAt, string receiptReference)
    {
        if (Status != PettyCashReturnStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash returns can be confirmed as received.");
        }

        if (SubmittedAt is { } submittedAt && receivedAt < submittedAt)
        {
            throw new DomainValidationException("The head-office receipt time cannot be before the return was submitted.");
        }

        ReceivedByUserId = receivedByUserId == Guid.Empty
            ? throw new DomainValidationException("The head-office receiver is required.")
            : receivedByUserId;
        ReceiptReference = Guard.NotNullOrWhiteSpace(receiptReference, nameof(receiptReference), maxLength: 128);
        ReceivedAt = receivedAt;
        Status = PettyCashReturnStatus.Received;
    }

    public void Reject(Guid rejectedByUserId, DateTimeOffset rejectedAt, string reason)
    {
        if (Status != PettyCashReturnStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted petty cash returns can be rejected.");
        }

        RejectedByUserId = rejectedByUserId == Guid.Empty
            ? throw new DomainValidationException("The head-office reviewer is required.")
            : rejectedByUserId;
        RejectionReason = Guard.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 512);
        RejectedAt = rejectedAt;
        Status = PettyCashReturnStatus.Rejected;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        EnsureDraft();
        CancelledAt = cancelledAt;
        Status = PettyCashReturnStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != PettyCashReturnStatus.Draft)
        {
            throw new DomainValidationException("Only draft petty cash returns can be changed.");
        }
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.NotNullOrWhiteSpace(value, paramName, maxLength: maxLength);
}

public sealed class PettyCashReturnLine : Entity
{
    private PettyCashReturnLine() { }

    public PettyCashReturnLine(Guid pettyCashReturnId, Guid pettyCashRequestLineId, decimal amount)
    {
        PettyCashReturnId = pettyCashReturnId;
        PettyCashRequestLineId = pettyCashRequestLineId == Guid.Empty
            ? throw new DomainValidationException("A funded category is required.")
            : pettyCashRequestLineId;
        Amount = Guard.Positive(amount, nameof(amount));
    }

    public Guid PettyCashReturnId { get; private set; }
    public Guid PettyCashRequestLineId { get; private set; }
    public decimal Amount { get; private set; }
}

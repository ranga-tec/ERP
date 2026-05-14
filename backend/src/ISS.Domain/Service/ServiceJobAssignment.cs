using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobAssignmentApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public sealed class ServiceJobAssignment : AuditableEntity
{
    private ServiceJobAssignment() { }

    public ServiceJobAssignment(
        Guid serviceJobId,
        Guid? technicianId,
        string employeeName,
        string role,
        string assignedTask,
        DateTimeOffset assignedDate,
        DateTimeOffset? workStartAt,
        DateTimeOffset? workEndAt,
        decimal normalHours,
        decimal overtimeHours,
        string? dailyWorkDescription)
    {
        ServiceJobId = serviceJobId;
        TechnicianId = technicianId;
        EmployeeName = Guard.NotNullOrWhiteSpace(employeeName, nameof(employeeName), 256);
        Role = Guard.NotNullOrWhiteSpace(role, nameof(role), 128);
        AssignedTask = Guard.NotNullOrWhiteSpace(assignedTask, nameof(assignedTask), 1000);
        AssignedDate = assignedDate;
        WorkStartAt = workStartAt;
        WorkEndAt = workEndAt;
        NormalHours = Guard.NotNegative(normalHours, nameof(normalHours));
        OvertimeHours = Guard.NotNegative(overtimeHours, nameof(overtimeHours));
        DailyWorkDescription = NormalizeOptional(dailyWorkDescription, nameof(dailyWorkDescription), 2000);
        ApprovalStatus = ServiceJobAssignmentApprovalStatus.Pending;
    }

    public Guid ServiceJobId { get; private set; }
    public Guid? TechnicianId { get; private set; }
    public string EmployeeName { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public string AssignedTask { get; private set; } = null!;
    public DateTimeOffset AssignedDate { get; private set; }
    public DateTimeOffset? WorkStartAt { get; private set; }
    public DateTimeOffset? WorkEndAt { get; private set; }
    public decimal NormalHours { get; private set; }
    public decimal OvertimeHours { get; private set; }
    public string? DailyWorkDescription { get; private set; }
    public ServiceJobAssignmentApprovalStatus ApprovalStatus { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public void Approve(DateTimeOffset approvedAt)
    {
        if (ApprovalStatus != ServiceJobAssignmentApprovalStatus.Pending)
        {
            throw new DomainValidationException("Only pending job assignments can be approved.");
        }

        ApprovalStatus = ServiceJobAssignmentApprovalStatus.Approved;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? reason)
    {
        if (ApprovalStatus != ServiceJobAssignmentApprovalStatus.Pending)
        {
            throw new DomainValidationException("Only pending job assignments can be rejected.");
        }

        ApprovalStatus = ServiceJobAssignmentApprovalStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = NormalizeOptional(reason, nameof(reason), 512);
        ApprovedAt = null;
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(value, paramName, maxLength);
    }
}

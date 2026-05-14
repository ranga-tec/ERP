using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobDailySheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public sealed class ServiceJobDailySheet : AuditableEntity
{
    private ServiceJobDailySheet() { }

    public ServiceJobDailySheet(
        string number,
        Guid serviceJobId,
        DateTimeOffset sheetDate,
        string preparedByName,
        string? siteLocation,
        string? shiftName,
        string? weatherOrSiteCondition,
        string workPlanned,
        string? workCompleted,
        string? workPending,
        string? problemsFound,
        string? customerInstructions,
        string? technicianNotes,
        string? supervisorNotes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), 32);
        ServiceJobId = serviceJobId;
        SheetDate = sheetDate;
        PreparedByName = Guard.NotNullOrWhiteSpace(preparedByName, nameof(preparedByName), 256);
        SiteLocation = NormalizeOptional(siteLocation, nameof(siteLocation), 512);
        ShiftName = NormalizeOptional(shiftName, nameof(shiftName), 128);
        WeatherOrSiteCondition = NormalizeOptional(weatherOrSiteCondition, nameof(weatherOrSiteCondition), 512);
        WorkPlanned = Guard.NotNullOrWhiteSpace(workPlanned, nameof(workPlanned), 2000);
        WorkCompleted = NormalizeOptional(workCompleted, nameof(workCompleted), 2000);
        WorkPending = NormalizeOptional(workPending, nameof(workPending), 2000);
        ProblemsFound = NormalizeOptional(problemsFound, nameof(problemsFound), 2000);
        CustomerInstructions = NormalizeOptional(customerInstructions, nameof(customerInstructions), 2000);
        TechnicianNotes = NormalizeOptional(technicianNotes, nameof(technicianNotes), 2000);
        SupervisorNotes = NormalizeOptional(supervisorNotes, nameof(supervisorNotes), 2000);
        Status = ServiceJobDailySheetStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public Guid ServiceJobId { get; private set; }
    public DateTimeOffset SheetDate { get; private set; }
    public string PreparedByName { get; private set; } = null!;
    public string? SiteLocation { get; private set; }
    public string? ShiftName { get; private set; }
    public string? WeatherOrSiteCondition { get; private set; }
    public string WorkPlanned { get; private set; } = null!;
    public string? WorkCompleted { get; private set; }
    public string? WorkPending { get; private set; }
    public string? ProblemsFound { get; private set; }
    public string? CustomerInstructions { get; private set; }
    public string? TechnicianNotes { get; private set; }
    public string? SupervisorNotes { get; private set; }
    public ServiceJobDailySheetStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != ServiceJobDailySheetStatus.Draft)
        {
            throw new DomainValidationException("Only draft daily sheets can be submitted.");
        }

        Status = ServiceJobDailySheetStatus.Submitted;
        SubmittedAt = submittedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        if (Status != ServiceJobDailySheetStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted daily sheets can be approved.");
        }

        Status = ServiceJobDailySheetStatus.Approved;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? reason)
    {
        if (Status != ServiceJobDailySheetStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted daily sheets can be rejected.");
        }

        Status = ServiceJobDailySheetStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = NormalizeOptional(reason, nameof(reason), 512);
        ApprovedAt = null;
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Guard.NotNullOrWhiteSpace(value, paramName, maxLength);
}

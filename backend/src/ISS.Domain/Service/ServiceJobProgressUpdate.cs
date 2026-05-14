using ISS.Domain.Common;

namespace ISS.Domain.Service;

public sealed class ServiceJobProgressUpdate : AuditableEntity
{
    private ServiceJobProgressUpdate() { }

    public ServiceJobProgressUpdate(
        Guid serviceJobId,
        DateTimeOffset progressDate,
        string workCompleted,
        string? workPending,
        string? problemsFound,
        string? additionalPartsRequired,
        string? additionalLaborRequired,
        string? customerInstructions,
        string? siteIssues,
        string? technicianNotes,
        string? supervisorNotes,
        Guid? serviceJobDailySheetId = null)
    {
        ServiceJobId = serviceJobId;
        ServiceJobDailySheetId = serviceJobDailySheetId;
        ProgressDate = progressDate;
        WorkCompleted = Guard.NotNullOrWhiteSpace(workCompleted, nameof(workCompleted), 2000);
        WorkPending = NormalizeOptional(workPending, nameof(workPending), 2000);
        ProblemsFound = NormalizeOptional(problemsFound, nameof(problemsFound), 2000);
        AdditionalPartsRequired = NormalizeOptional(additionalPartsRequired, nameof(additionalPartsRequired), 2000);
        AdditionalLaborRequired = NormalizeOptional(additionalLaborRequired, nameof(additionalLaborRequired), 2000);
        CustomerInstructions = NormalizeOptional(customerInstructions, nameof(customerInstructions), 2000);
        SiteIssues = NormalizeOptional(siteIssues, nameof(siteIssues), 2000);
        TechnicianNotes = NormalizeOptional(technicianNotes, nameof(technicianNotes), 2000);
        SupervisorNotes = NormalizeOptional(supervisorNotes, nameof(supervisorNotes), 2000);
    }

    public Guid ServiceJobId { get; private set; }
    public Guid? ServiceJobDailySheetId { get; private set; }
    public DateTimeOffset ProgressDate { get; private set; }
    public string WorkCompleted { get; private set; } = null!;
    public string? WorkPending { get; private set; }
    public string? ProblemsFound { get; private set; }
    public string? AdditionalPartsRequired { get; private set; }
    public string? AdditionalLaborRequired { get; private set; }
    public string? CustomerInstructions { get; private set; }
    public string? SiteIssues { get; private set; }
    public string? TechnicianNotes { get; private set; }
    public string? SupervisorNotes { get; private set; }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(value, paramName, maxLength);
    }
}

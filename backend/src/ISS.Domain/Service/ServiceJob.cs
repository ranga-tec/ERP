using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobKind
{
    Service = 0,
    Repair = 1,
    Pdi = 2,
    Warranty = 3,
    Inspection = 4
}

public enum ServiceJobStatus
{
    Draft = 0,
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    WaitingForParts = 4,
    WaitingForCustomerApproval = 5,
    WaitingForSupplier = 6,
    WorkCompleted = 7,
    PendingExpenseSettlement = 8,
    PendingMaterialReturn = 9,
    ReadyForInvoice = 10,
    Invoiced = 11,
    Closed = 12,
    Reopened = 13,
    Cancelled = 14
}

public sealed class ServiceJob : AuditableEntity
{
    private ServiceJob() { }

    public ServiceJob(
        string number,
        Guid equipmentUnitId,
        Guid customerId,
        DateTimeOffset openedAt,
        string problemDescription,
        ServiceJobKind kind = ServiceJobKind.Service,
        Guid? serviceContractId = null,
        ServiceEntitlementSource entitlementSource = ServiceEntitlementSource.None,
        ServiceCoverageScope entitlementCoverage = ServiceCoverageScope.None,
        CustomerBillingTreatment customerBillingTreatment = CustomerBillingTreatment.Billable,
        DateTimeOffset? entitlementEvaluatedAt = null,
        string? entitlementSummary = null,
        DateTimeOffset? expectedCompletionAt = null,
        string? siteLocation = null,
        DateTimeOffset? estimatedStartAt = null,
        string? jobDescription = null,
        string? customerComplaint = null,
        string? internalRemarks = null,
        string? responsibleOfficerName = null)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        EquipmentUnitId = equipmentUnitId;
        CustomerId = customerId;
        OpenedAt = openedAt;
        ProblemDescription = Guard.NotNullOrWhiteSpace(problemDescription, nameof(ProblemDescription), maxLength: 2000);
        Kind = kind;
        Status = ServiceJobStatus.Open;
        EstimatedStartAt = estimatedStartAt;
        ExpectedCompletionAt = expectedCompletionAt;
        SiteLocation = NormalizeOptional(siteLocation, nameof(siteLocation), 512);
        JobDescription = NormalizeOptional(jobDescription, nameof(jobDescription), 2000);
        CustomerComplaint = NormalizeOptional(customerComplaint, nameof(customerComplaint), 2000);
        InternalRemarks = NormalizeOptional(internalRemarks, nameof(internalRemarks), 2000);
        ResponsibleOfficerName = NormalizeOptional(responsibleOfficerName, nameof(responsibleOfficerName), 256);
        ApplyEntitlement(
            serviceContractId,
            entitlementSource,
            entitlementCoverage,
            customerBillingTreatment,
            entitlementEvaluatedAt,
            entitlementSummary);
    }

    public string Number { get; private set; } = null!;
    public Guid EquipmentUnitId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public string ProblemDescription { get; private set; } = null!;
    public ServiceJobKind Kind { get; private set; }
    public ServiceJobStatus Status { get; private set; }
    public DateTimeOffset? EstimatedStartAt { get; private set; }
    public DateTimeOffset? ActualStartAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ExpectedCompletionAt { get; private set; }
    public string? SiteLocation { get; private set; }
    public string? JobDescription { get; private set; }
    public string? CustomerComplaint { get; private set; }
    public string? InternalRemarks { get; private set; }
    public string? ResponsibleOfficerName { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public ServiceEntitlementSource EntitlementSource { get; private set; }
    public ServiceCoverageScope EntitlementCoverage { get; private set; }
    public CustomerBillingTreatment CustomerBillingTreatment { get; private set; }
    public DateTimeOffset? EntitlementEvaluatedAt { get; private set; }
    public string? EntitlementSummary { get; private set; }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status is not (ServiceJobStatus.Draft or ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.Reopened))
        {
            throw new DomainValidationException("Only draft, open, assigned, or reopened service jobs can be started.");
        }

        Status = ServiceJobStatus.InProgress;
        ActualStartAt ??= startedAt;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is not (ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.InProgress or ServiceJobStatus.Reopened))
        {
            throw new DomainValidationException("Service job must be open, assigned, in progress, or reopened to complete.");
        }

        Status = ServiceJobStatus.WorkCompleted;
        ActualStartAt ??= completedAt;
        CompletedAt = completedAt;
    }

    public void Close()
    {
        if (Status is not (ServiceJobStatus.WorkCompleted or ServiceJobStatus.ReadyForInvoice or ServiceJobStatus.Invoiced))
        {
            throw new DomainValidationException("Only work-completed, ready-for-invoice, or invoiced service jobs can be closed.");
        }

        Status = ServiceJobStatus.Closed;
    }

    public void Reopen(string? reason)
    {
        if (Status != ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Only closed service jobs can be reopened.");
        }

        Status = ServiceJobStatus.Reopened;
        var normalizedReason = NormalizeOptional(reason, nameof(reason), 1000);
        if (normalizedReason is not null)
        {
            InternalRemarks = string.IsNullOrWhiteSpace(InternalRemarks)
                ? $"Reopened: {normalizedReason}"
                : $"{InternalRemarks}{Environment.NewLine}Reopened: {normalizedReason}";
        }
    }

    public void Cancel()
    {
        if (Status == ServiceJobStatus.Closed)
        {
            throw new DomainValidationException("Closed service jobs cannot be cancelled.");
        }

        Status = ServiceJobStatus.Cancelled;
    }

    public void ApplyEntitlement(
        Guid? serviceContractId,
        ServiceEntitlementSource entitlementSource,
        ServiceCoverageScope entitlementCoverage,
        CustomerBillingTreatment customerBillingTreatment,
        DateTimeOffset? entitlementEvaluatedAt,
        string? entitlementSummary)
    {
        if (entitlementSource == ServiceEntitlementSource.None)
        {
            if (serviceContractId is not null)
            {
                throw new DomainValidationException("Service contract cannot be linked when entitlement source is None.");
            }

            if (entitlementCoverage != ServiceCoverageScope.None)
            {
                throw new DomainValidationException("Entitlement coverage must be None when no entitlement source is set.");
            }

            if (customerBillingTreatment != CustomerBillingTreatment.Billable)
            {
                throw new DomainValidationException("Jobs without entitlement must remain billable.");
            }
        }
        else
        {
            if (entitlementCoverage == ServiceCoverageScope.None)
            {
                throw new DomainValidationException("Covered jobs must specify an entitlement coverage scope.");
            }

            if (entitlementSource == ServiceEntitlementSource.ServiceContract && serviceContractId is null)
            {
                throw new DomainValidationException("Service-contract entitlement must link to a contract.");
            }

            if (entitlementSource != ServiceEntitlementSource.ServiceContract && serviceContractId is not null)
            {
                throw new DomainValidationException("Only service-contract entitlement may link to a contract.");
            }
        }

        ServiceContractId = entitlementSource == ServiceEntitlementSource.ServiceContract ? serviceContractId : null;
        EntitlementSource = entitlementSource;
        EntitlementCoverage = entitlementCoverage;
        CustomerBillingTreatment = customerBillingTreatment;
        EntitlementEvaluatedAt = entitlementEvaluatedAt;
        EntitlementSummary = string.IsNullOrWhiteSpace(entitlementSummary)
            ? null
            : Guard.NotNullOrWhiteSpace(entitlementSummary, nameof(entitlementSummary), maxLength: 512);
    }

    public void Update(
        Guid equipmentUnitId,
        Guid customerId,
        string problemDescription,
        ServiceJobKind kind,
        DateTimeOffset? expectedCompletionAt = null,
        string? siteLocation = null,
        DateTimeOffset? estimatedStartAt = null,
        string? jobDescription = null,
        string? customerComplaint = null,
        string? internalRemarks = null,
        string? responsibleOfficerName = null)
    {
        if (Status is not (ServiceJobStatus.Open or ServiceJobStatus.Draft or ServiceJobStatus.Reopened))
        {
            throw new DomainValidationException("Only draft, open, or reopened service jobs can be edited.");
        }

        EquipmentUnitId = equipmentUnitId;
        CustomerId = customerId;
        ProblemDescription = Guard.NotNullOrWhiteSpace(problemDescription, nameof(problemDescription), maxLength: 2000);
        Kind = kind;
        EstimatedStartAt = estimatedStartAt;
        ExpectedCompletionAt = expectedCompletionAt;
        SiteLocation = NormalizeOptional(siteLocation, nameof(siteLocation), 512);
        JobDescription = NormalizeOptional(jobDescription, nameof(jobDescription), 2000);
        CustomerComplaint = NormalizeOptional(customerComplaint, nameof(customerComplaint), 2000);
        InternalRemarks = NormalizeOptional(internalRemarks, nameof(internalRemarks), 2000);
        ResponsibleOfficerName = NormalizeOptional(responsibleOfficerName, nameof(responsibleOfficerName), 256);
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Guard.NotNullOrWhiteSpace(value, paramName, maxLength: maxLength);
}

using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceJobKind
{
    Service = 0,
    Repair = 1
}

public enum ServiceJobStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Closed = 3,
    Cancelled = 4
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
        string? entitlementSummary = null)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), maxLength: 32);
        EquipmentUnitId = equipmentUnitId;
        CustomerId = customerId;
        OpenedAt = openedAt;
        ProblemDescription = Guard.NotNullOrWhiteSpace(problemDescription, nameof(ProblemDescription), maxLength: 2000);
        Kind = kind;
        Status = ServiceJobStatus.Open;
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
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public ServiceEntitlementSource EntitlementSource { get; private set; }
    public ServiceCoverageScope EntitlementCoverage { get; private set; }
    public CustomerBillingTreatment CustomerBillingTreatment { get; private set; }
    public DateTimeOffset? EntitlementEvaluatedAt { get; private set; }
    public string? EntitlementSummary { get; private set; }

    public void Start()
    {
        if (Status != ServiceJobStatus.Open)
        {
            throw new DomainValidationException("Only open service jobs can be started.");
        }

        Status = ServiceJobStatus.InProgress;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is not (ServiceJobStatus.Open or ServiceJobStatus.InProgress))
        {
            throw new DomainValidationException("Service job must be open or in progress to complete.");
        }

        Status = ServiceJobStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Close()
    {
        if (Status != ServiceJobStatus.Completed)
        {
            throw new DomainValidationException("Only completed service jobs can be closed.");
        }

        Status = ServiceJobStatus.Closed;
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
}

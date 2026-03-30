using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum ServiceContractType
{
    AnnualMaintenance = 0,
    ServiceLevelAgreement = 1,
    WarrantyExtension = 2
}

public sealed class ServiceContract : AuditableEntity
{
    private ServiceContract() { }

    public ServiceContract(
        string number,
        Guid customerId,
        Guid equipmentUnitId,
        ServiceContractType contractType,
        ServiceCoverageScope coverage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes)
    {
        Number = Guard.NotNullOrWhiteSpace(number, nameof(number), maxLength: 32);
        CustomerId = customerId;
        EquipmentUnitId = equipmentUnitId;
        ContractType = contractType;
        ApplyScheduleAndCoverage(coverage, startDate, endDate);
        Notes = NormalizeNotes(notes);
        IsActive = true;
    }

    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid EquipmentUnitId { get; private set; }
    public ServiceContractType ContractType { get; private set; }
    public ServiceCoverageScope Coverage { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        Guid customerId,
        Guid equipmentUnitId,
        ServiceContractType contractType,
        ServiceCoverageScope coverage,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes,
        bool isActive)
    {
        CustomerId = customerId;
        EquipmentUnitId = equipmentUnitId;
        ContractType = contractType;
        ApplyScheduleAndCoverage(coverage, startDate, endDate);
        Notes = NormalizeNotes(notes);
        IsActive = isActive;
    }

    public bool IsCovering(DateTimeOffset when)
        => IsActive && StartDate <= when && EndDate >= when;

    private void ApplyScheduleAndCoverage(
        ServiceCoverageScope coverage,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        if (coverage == ServiceCoverageScope.None)
        {
            throw new DomainValidationException("Service contract coverage cannot be None.");
        }

        if (endDate < startDate)
        {
            throw new DomainValidationException("Service contract end date cannot be earlier than the start date.");
        }

        Coverage = coverage;
        StartDate = startDate;
        EndDate = endDate;
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes)
            ? null
            : Guard.NotNullOrWhiteSpace(notes, nameof(notes), maxLength: 2000);
}

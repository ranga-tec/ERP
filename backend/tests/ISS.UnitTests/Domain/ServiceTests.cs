using ISS.Domain.Common;
using ISS.Domain.Service;

namespace ISS.UnitTests.Domain;

public sealed class ServiceTests
{
    [Fact]
    public void ServiceJob_Transitions_Are_Validated()
    {
        var job = new ServiceJob("SJ0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "Won't start");

        job.Start();
        Assert.Equal(ServiceJobStatus.InProgress, job.Status);

        job.Complete(DateTimeOffset.UtcNow);
        Assert.Equal(ServiceJobStatus.Completed, job.Status);

        job.Close();
        Assert.Equal(ServiceJobStatus.Closed, job.Status);

        Assert.Throws<DomainValidationException>(() => job.Cancel());
    }

    [Fact]
    public void EquipmentUnit_Warranty_Coverage_Requires_End_Date()
    {
        Assert.Throws<DomainValidationException>(() => new EquipmentUnit(
            Guid.NewGuid(),
            "SN-001",
            Guid.NewGuid(),
            purchasedAt: null,
            warrantyUntil: null,
            warrantyCoverage: ServiceCoverageScope.LaborOnly));

        var unit = new EquipmentUnit(
            Guid.NewGuid(),
            "SN-002",
            Guid.NewGuid(),
            purchasedAt: null,
            warrantyUntil: DateTimeOffset.UtcNow.AddDays(90),
            warrantyCoverage: ServiceCoverageScope.LaborAndParts);

        Assert.True(unit.HasActiveWarranty(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ServiceContract_Validation_Enforces_Coverage_And_Date_Window()
    {
        var start = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(30);
        var contract = new ServiceContract(
            "SC0001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceContractType.AnnualMaintenance,
            ServiceCoverageScope.PartsOnly,
            start,
            end,
            "Coverage notes");

        Assert.True(contract.IsCovering(start.AddDays(1)));

        Assert.Throws<DomainValidationException>(() => new ServiceContract(
            "SC0002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceContractType.ServiceLevelAgreement,
            ServiceCoverageScope.None,
            start,
            end,
            null));

        Assert.Throws<DomainValidationException>(() => new ServiceContract(
            "SC0003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceContractType.WarrantyExtension,
            ServiceCoverageScope.LaborOnly,
            end,
            start,
            null));
    }

    [Fact]
    public void ServiceJob_Entitlement_Invariants_Are_Validated()
    {
        var contractId = Guid.NewGuid();
        var evaluatedAt = DateTimeOffset.UtcNow;

        var job = new ServiceJob(
            "SJ0002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            evaluatedAt,
            "Warranty check",
            ServiceJobKind.Repair,
            contractId,
            ServiceEntitlementSource.ServiceContract,
            ServiceCoverageScope.LaborOnly,
            CustomerBillingTreatment.PartiallyCovered,
            evaluatedAt,
            "Contract coverage");

        Assert.Equal(contractId, job.ServiceContractId);
        Assert.Equal(ServiceEntitlementSource.ServiceContract, job.EntitlementSource);

        Assert.Throws<DomainValidationException>(() => job.ApplyEntitlement(
            serviceContractId: contractId,
            entitlementSource: ServiceEntitlementSource.None,
            entitlementCoverage: ServiceCoverageScope.None,
            customerBillingTreatment: CustomerBillingTreatment.Billable,
            entitlementEvaluatedAt: evaluatedAt,
            entitlementSummary: null));
    }

    [Fact]
    public void MaterialRequisition_Post_Requires_Lines()
    {
        var mr = new MaterialRequisition("MR0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => mr.Post());
        mr.AddLine(Guid.NewGuid(), 1m, batchNumber: "B1");
        mr.Post();
        Assert.Equal(MaterialRequisitionStatus.Posted, mr.Status);
    }

    [Fact]
    public void WorkOrder_TimeEntry_Approval_And_Invoicing_Follow_State_Rules()
    {
        var workOrder = new WorkOrder(Guid.NewGuid(), "Inspect compressor", assignedToUserId: null);
        var entry = workOrder.AddTimeEntry(
            technicianUserId: null,
            technicianName: "Tech A",
            workDate: new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero),
            workDescription: "Diagnosis and pressure test",
            hoursWorked: 2m,
            costRate: 15m,
            billableToCustomer: true,
            billableHours: 1.5m,
            billingRate: 30m,
            taxPercent: 10m,
            notes: "Initial bench test");

        Assert.Equal(30m, entry.LaborCost);
        Assert.Equal(49.5m, entry.BillableTotal);

        entry.Submit(DateTimeOffset.UtcNow);
        entry.Approve(DateTimeOffset.UtcNow);

        Assert.Throws<DomainValidationException>(() => entry.Update(
            technicianUserId: null,
            technicianName: "Tech A",
            workDate: entry.WorkDate,
            workDescription: entry.WorkDescription,
            hoursWorked: entry.HoursWorked,
            costRate: entry.CostRate,
            billableToCustomer: entry.BillableToCustomer,
            billableHours: entry.BillableHours,
            billingRate: entry.BillingRate,
            taxPercent: entry.TaxPercent,
            notes: entry.Notes));

        var invoiceId = Guid.NewGuid();
        var invoiceLineId = Guid.NewGuid();
        entry.MarkInvoiced(invoiceId, invoiceLineId, DateTimeOffset.UtcNow);

        Assert.Equal(WorkOrderTimeEntryStatus.Invoiced, entry.Status);
        Assert.Equal(invoiceId, entry.SalesInvoiceId);
        Assert.Equal(invoiceLineId, entry.SalesInvoiceLineId);
        Assert.Throws<DomainValidationException>(() => entry.MarkInvoiced(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}

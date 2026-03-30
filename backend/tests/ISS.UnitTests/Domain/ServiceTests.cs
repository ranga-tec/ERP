using ISS.Domain.Common;
using ISS.Domain.Service;

namespace ISS.UnitTests.Domain;

public sealed class ServiceTests
{
    [Fact]
    public void ServiceJob_Transitions_Are_Validated()
    {
        var job = new ServiceJob("SJ0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "Won't start");

        job.Update(job.EquipmentUnitId, job.CustomerId, "Still won't start", ServiceJobKind.Service);
        Assert.Equal("Still won't start", job.ProblemDescription);

        job.Start();
        Assert.Equal(ServiceJobStatus.InProgress, job.Status);

        job.Complete(DateTimeOffset.UtcNow);
        Assert.Equal(ServiceJobStatus.Completed, job.Status);

        job.Close();
        Assert.Equal(ServiceJobStatus.Closed, job.Status);

        Assert.Throws<DomainValidationException>(() => job.Cancel());
        Assert.Throws<DomainValidationException>(() => job.Update(Guid.NewGuid(), Guid.NewGuid(), "Too late", ServiceJobKind.Repair));
    }

    [Fact]
    public void ServiceEstimate_Header_Is_Editable_Only_In_Draft()
    {
        var estimate = new ServiceEstimate(
            "SE0001",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            "Initial terms");

        estimate.UpdateHeader(DateTimeOffset.UtcNow.AddDays(14), "Updated terms");
        Assert.Equal("Updated terms", estimate.Terms);

        estimate.AddLine(ServiceEstimateLineKind.Labor, null, "Inspection", 1m, 10m, 0m);
        estimate.Approve();

        Assert.Throws<DomainValidationException>(() => estimate.UpdateHeader(DateTimeOffset.UtcNow.AddDays(21), "Too late"));
    }

    [Fact]
    public void ServiceEstimate_Sent_For_Customer_Approval_Resets_When_Draft_Is_Edited()
    {
        var issuedAt = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero);
        var estimate = new ServiceEstimate(
            "SE0002",
            Guid.NewGuid(),
            issuedAt,
            issuedAt.AddDays(7),
            "Initial terms");

        estimate.AddLine(ServiceEstimateLineKind.Part, Guid.NewGuid(), "Filter", 1m, 50m, 0m);

        var sentAt = issuedAt.AddHours(2);
        estimate.MarkSentToCustomer(sentAt);

        Assert.Equal(ServiceEstimateCustomerApprovalStatus.Pending, estimate.CustomerApprovalStatus);
        Assert.Equal(sentAt, estimate.SentToCustomerAt);
        Assert.Null(estimate.CustomerDecisionAt);

        estimate.UpdateHeader(issuedAt.AddDays(10), "Revised terms");

        Assert.Equal(ServiceEstimateCustomerApprovalStatus.NotSent, estimate.CustomerApprovalStatus);
        Assert.Null(estimate.SentToCustomerAt);
        Assert.Null(estimate.CustomerDecisionAt);
    }

    [Fact]
    public void ServiceEstimate_Approval_Tracks_Customer_Decision_Timestamp()
    {
        var issuedAt = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero);
        var estimate = new ServiceEstimate(
            "SE0003",
            Guid.NewGuid(),
            issuedAt,
            issuedAt.AddDays(7),
            "Terms");

        estimate.AddLine(ServiceEstimateLineKind.Labor, null, "Inspection", 2m, 25m, 0m);
        estimate.MarkSentToCustomer(issuedAt.AddHours(1));

        var decisionAt = issuedAt.AddHours(5);
        estimate.Approve(decisionAt);

        Assert.Equal(ServiceEstimateStatus.Approved, estimate.Status);
        Assert.Equal(ServiceEstimateCustomerApprovalStatus.Approved, estimate.CustomerApprovalStatus);
        Assert.Equal(decisionAt, estimate.CustomerDecisionAt);
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
    public void ServiceEntitlementRules_Zero_Covered_Labor_And_Parts()
    {
        Assert.Equal(0m, ServiceEntitlementRules.ApplyEstimateUnitPrice(ServiceCoverageScope.LaborOnly, ServiceEstimateLineKind.Labor, 40m));
        Assert.Equal(0m, ServiceEntitlementRules.ApplyEstimateUnitPrice(ServiceCoverageScope.PartsOnly, ServiceEstimateLineKind.Part, 25m));
        Assert.Equal(30m, ServiceEntitlementRules.ApplyEstimateUnitPrice(ServiceCoverageScope.PartsOnly, ServiceEstimateLineKind.Labor, 30m));
        Assert.Equal(15m, ServiceEntitlementRules.ApplyEstimateUnitPrice(ServiceCoverageScope.LaborAndParts, ServiceEstimateLineKind.Expense, 15m));
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

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

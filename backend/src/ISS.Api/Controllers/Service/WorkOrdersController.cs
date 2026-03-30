using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/work-orders")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
public sealed class WorkOrdersController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record WorkOrderTimeEntryDto(
        Guid Id,
        Guid WorkOrderId,
        Guid ServiceJobId,
        Guid? TechnicianUserId,
        string TechnicianName,
        DateTimeOffset WorkDate,
        string WorkDescription,
        decimal HoursWorked,
        decimal CostRate,
        decimal LaborCost,
        bool BillableToCustomer,
        decimal BillableHours,
        decimal BillingRate,
        decimal TaxPercent,
        decimal BillableTotal,
        string? Notes,
        WorkOrderTimeEntryStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? RejectedAt,
        string? RejectionReason,
        Guid? SalesInvoiceId,
        Guid? SalesInvoiceLineId,
        DateTimeOffset? InvoicedAt);

    public sealed record WorkOrderSummaryDto(
        Guid Id,
        Guid ServiceJobId,
        string Description,
        Guid? AssignedToUserId,
        WorkOrderStatus Status,
        int TimeEntryCount,
        decimal ApprovedHours,
        decimal ApprovedLaborCost);

    public sealed record WorkOrderDto(
        Guid Id,
        Guid ServiceJobId,
        string Description,
        Guid? AssignedToUserId,
        WorkOrderStatus Status,
        decimal ApprovedHours,
        decimal ApprovedLaborCost,
        decimal PendingLaborCost,
        decimal BillableApprovedAmount,
        IReadOnlyList<WorkOrderTimeEntryDto> TimeEntries);

    public sealed record CreateWorkOrderRequest(Guid ServiceJobId, string Description, Guid? AssignedToUserId);

    public sealed record AddWorkOrderTimeEntryRequest(
        Guid? TechnicianUserId,
        string? TechnicianName,
        DateTimeOffset? WorkDate,
        string WorkDescription,
        decimal HoursWorked,
        decimal CostRate,
        bool BillableToCustomer,
        decimal? BillableHours,
        decimal? BillingRate,
        decimal? TaxPercent,
        string? Notes);

    public sealed record UpdateWorkOrderTimeEntryRequest(
        Guid? TechnicianUserId,
        string? TechnicianName,
        DateTimeOffset? WorkDate,
        string WorkDescription,
        decimal HoursWorked,
        decimal CostRate,
        bool BillableToCustomer,
        decimal? BillableHours,
        decimal? BillingRate,
        decimal? TaxPercent,
        string? Notes);

    public sealed record RejectWorkOrderTimeEntryRequest(string? RejectionReason);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkOrderSummaryDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var workOrders = await dbContext.WorkOrders.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new WorkOrderSummaryDto(
                x.Id,
                x.ServiceJobId,
                x.Description,
                x.AssignedToUserId,
                x.Status,
                x.TimeEntries.Count,
                x.TimeEntries
                    .Where(entry => entry.Status == WorkOrderTimeEntryStatus.Approved || entry.Status == WorkOrderTimeEntryStatus.Invoiced)
                    .Sum(entry => entry.HoursWorked),
                x.TimeEntries
                    .Where(entry => entry.Status == WorkOrderTimeEntryStatus.Approved || entry.Status == WorkOrderTimeEntryStatus.Invoiced)
                    .Sum(entry => entry.HoursWorked * entry.CostRate)))
            .ToListAsync(cancellationToken);

        return Ok(workOrders);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDto>> Create(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateWorkOrderAsync(
            request.ServiceJobId,
            request.Description,
            request.AssignedToUserId,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.AsNoTracking()
            .Include(x => x.TimeEntries)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return workOrder is null ? NotFound() : Ok(MapWorkOrder(workOrder));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.WorkOrder, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/time-entries")]
    public async Task<ActionResult<WorkOrderDto>> AddTimeEntry(
        Guid id,
        AddWorkOrderTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.AddWorkOrderTimeEntryAsync(
            id,
            request.TechnicianUserId,
            ResolveTechnicianName(request.TechnicianName),
            request.WorkDate,
            request.WorkDescription,
            request.HoursWorked,
            request.CostRate,
            request.BillableToCustomer,
            request.BillableHours,
            request.BillingRate,
            request.TaxPercent,
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}/time-entries/{timeEntryId:guid}")]
    public async Task<ActionResult<WorkOrderDto>> UpdateTimeEntry(
        Guid id,
        Guid timeEntryId,
        UpdateWorkOrderTimeEntryRequest request,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateWorkOrderTimeEntryAsync(
            id,
            timeEntryId,
            request.TechnicianUserId,
            ResolveTechnicianName(request.TechnicianName),
            request.WorkDate,
            request.WorkDescription,
            request.HoursWorked,
            request.CostRate,
            request.BillableToCustomer,
            request.BillableHours,
            request.BillingRate,
            request.TaxPercent,
            request.Notes,
            cancellationToken);

        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}/time-entries/{timeEntryId:guid}")]
    public async Task<ActionResult> RemoveTimeEntry(Guid id, Guid timeEntryId, CancellationToken cancellationToken)
    {
        await serviceManagementService.RemoveWorkOrderTimeEntryAsync(id, timeEntryId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/time-entries/{timeEntryId:guid}/submit")]
    public async Task<ActionResult> SubmitTimeEntry(Guid id, Guid timeEntryId, CancellationToken cancellationToken)
    {
        await serviceManagementService.SubmitWorkOrderTimeEntryAsync(id, timeEntryId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/time-entries/{timeEntryId:guid}/approve")]
    public async Task<ActionResult> ApproveTimeEntry(Guid id, Guid timeEntryId, CancellationToken cancellationToken)
    {
        await serviceManagementService.ApproveWorkOrderTimeEntryAsync(id, timeEntryId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/time-entries/{timeEntryId:guid}/reject")]
    public async Task<ActionResult> RejectTimeEntry(
        Guid id,
        Guid timeEntryId,
        RejectWorkOrderTimeEntryRequest? request,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.RejectWorkOrderTimeEntryAsync(id, timeEntryId, request?.RejectionReason, cancellationToken);
        return NoContent();
    }

    private WorkOrderDto MapWorkOrder(WorkOrder workOrder)
    {
        var orderedEntries = workOrder.TimeEntries
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.Id)
            .Select(entry => new WorkOrderTimeEntryDto(
                entry.Id,
                entry.WorkOrderId,
                entry.ServiceJobId,
                entry.TechnicianUserId,
                entry.TechnicianName,
                entry.WorkDate,
                entry.WorkDescription,
                entry.HoursWorked,
                entry.CostRate,
                entry.LaborCost,
                entry.BillableToCustomer,
                entry.BillableHours,
                entry.BillingRate,
                entry.TaxPercent,
                entry.BillableTotal,
                entry.Notes,
                entry.Status,
                entry.SubmittedAt,
                entry.ApprovedAt,
                entry.RejectedAt,
                entry.RejectionReason,
                entry.SalesInvoiceId,
                entry.SalesInvoiceLineId,
                entry.InvoicedAt))
            .ToList();

        return new WorkOrderDto(
            workOrder.Id,
            workOrder.ServiceJobId,
            workOrder.Description,
            workOrder.AssignedToUserId,
            workOrder.Status,
            orderedEntries
                .Where(entry => entry.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced)
                .Sum(entry => entry.HoursWorked),
            orderedEntries
                .Where(entry => entry.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced)
                .Sum(entry => entry.LaborCost),
            orderedEntries
                .Where(entry => entry.Status == WorkOrderTimeEntryStatus.Submitted)
                .Sum(entry => entry.LaborCost),
            orderedEntries
                .Where(entry => entry.BillableToCustomer && (entry.Status is WorkOrderTimeEntryStatus.Approved or WorkOrderTimeEntryStatus.Invoiced))
                .Sum(entry => entry.BillableTotal),
            orderedEntries);
    }

    private string ResolveTechnicianName(string? requestedName)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName.Trim();
        }

        return User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Unknown technician";
    }
}

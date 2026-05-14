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
[Route("api/service/jobs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales},{Roles.Inventory}")]
public sealed class ServiceJobsController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    ServiceCostingService serviceCostingService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceJobDto(
        Guid Id,
        string Number,
        Guid EquipmentUnitId,
        Guid CustomerId,
        DateTimeOffset OpenedAt,
        string ProblemDescription,
        ServiceJobKind Kind,
        ServiceJobStatus Status,
        DateTimeOffset? EstimatedStartAt,
        DateTimeOffset? ActualStartAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? ExpectedCompletionAt,
        string? SiteLocation,
        string? JobDescription,
        string? CustomerComplaint,
        string? InternalRemarks,
        string? ResponsibleOfficerName,
        Guid? ServiceContractId,
        string? ServiceContractNumber,
        ServiceEntitlementSource EntitlementSource,
        ServiceCoverageScope EntitlementCoverage,
        CustomerBillingTreatment CustomerBillingTreatment,
        DateTimeOffset? EntitlementEvaluatedAt,
        string? EntitlementSummary);
    public sealed record CreateServiceJobRequest(
        Guid EquipmentUnitId,
        Guid CustomerId,
        string ProblemDescription,
        ServiceJobKind? Kind,
        DateTimeOffset? EstimatedStartAt,
        DateTimeOffset? ExpectedCompletionAt,
        string? SiteLocation,
        string? JobDescription,
        string? CustomerComplaint,
        string? InternalRemarks,
        string? ResponsibleOfficerName);
    public sealed record UpdateServiceJobRequest(
        Guid EquipmentUnitId,
        Guid CustomerId,
        string ProblemDescription,
        ServiceJobKind Kind,
        DateTimeOffset? EstimatedStartAt,
        DateTimeOffset? ExpectedCompletionAt,
        string? SiteLocation,
        string? JobDescription,
        string? CustomerComplaint,
        string? InternalRemarks,
        string? ResponsibleOfficerName);
    public sealed record ReopenServiceJobRequest(string? Reason);
    public sealed record ServiceJobAssignmentDto(
        Guid Id,
        Guid ServiceJobId,
        Guid? TechnicianId,
        string EmployeeName,
        string Role,
        string AssignedTask,
        DateTimeOffset AssignedDate,
        DateTimeOffset? WorkStartAt,
        DateTimeOffset? WorkEndAt,
        decimal NormalHours,
        decimal OvertimeHours,
        string? DailyWorkDescription,
        ServiceJobAssignmentApprovalStatus ApprovalStatus,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? RejectedAt,
        string? RejectionReason);
    public sealed record AddServiceJobAssignmentRequest(
        Guid? TechnicianId,
        string? EmployeeName,
        string Role,
        string AssignedTask,
        DateTimeOffset? AssignedDate,
        DateTimeOffset? WorkStartAt,
        DateTimeOffset? WorkEndAt,
        decimal NormalHours,
        decimal OvertimeHours,
        string? DailyWorkDescription);
    public sealed record RejectServiceJobAssignmentRequest(string? Reason);
    public sealed record ServiceJobProgressUpdateDto(
        Guid Id,
        Guid ServiceJobId,
        DateTimeOffset ProgressDate,
        string WorkCompleted,
        string? WorkPending,
        string? ProblemsFound,
        string? AdditionalPartsRequired,
        string? AdditionalLaborRequired,
        string? CustomerInstructions,
        string? SiteIssues,
        string? TechnicianNotes,
        string? SupervisorNotes,
        DateTimeOffset CreatedAt);
    public sealed record AddServiceJobProgressUpdateRequest(
        DateTimeOffset? ProgressDate,
        string WorkCompleted,
        string? WorkPending,
        string? ProblemsFound,
        string? AdditionalPartsRequired,
        string? AdditionalLaborRequired,
        string? CustomerInstructions,
        string? SiteIssues,
        string? TechnicianNotes,
        string? SupervisorNotes);
    public sealed record ServiceJobCloseoutCheckDto(string Key, string Label, bool IsClear, int PendingCount, string Detail);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceJobDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var jobs = await dbContext.ServiceJobs.AsNoTracking()
            .OrderByDescending(x => x.OpenedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceJobDto(
                x.Id,
                x.Number,
                x.EquipmentUnitId,
                x.CustomerId,
                x.OpenedAt,
                x.ProblemDescription,
                x.Kind,
                x.Status,
                x.EstimatedStartAt,
                x.ActualStartAt,
                x.CompletedAt,
                x.ExpectedCompletionAt,
                x.SiteLocation,
                x.JobDescription,
                x.CustomerComplaint,
                x.InternalRemarks,
                x.ResponsibleOfficerName,
                x.ServiceContractId,
                dbContext.ServiceContracts
                    .Where(contract => contract.Id == x.ServiceContractId)
                    .Select(contract => contract.Number)
                    .FirstOrDefault(),
                x.EntitlementSource,
                x.EntitlementCoverage,
                x.CustomerBillingTreatment,
                x.EntitlementEvaluatedAt,
                x.EntitlementSummary))
            .ToListAsync(cancellationToken);

        return Ok(jobs);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult<ServiceJobDto>> Create(CreateServiceJobRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceJobAsync(
            request.EquipmentUnitId,
            request.CustomerId,
            request.ProblemDescription,
            request.Kind ?? ServiceJobKind.Service,
            request.ExpectedCompletionAt,
            request.SiteLocation,
            request.EstimatedStartAt,
            request.JobDescription,
            request.CustomerComplaint,
            request.InternalRemarks,
            request.ResponsibleOfficerName,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult<ServiceJobDto>> Update(Guid id, UpdateServiceJobRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceJobAsync(
            id,
            request.EquipmentUnitId,
            request.CustomerId,
            request.ProblemDescription,
            request.Kind,
            request.ExpectedCompletionAt,
            request.SiteLocation,
            request.EstimatedStartAt,
            request.JobDescription,
            request.CustomerComplaint,
            request.InternalRemarks,
            request.ResponsibleOfficerName,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceJobDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ServiceJobs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ServiceJobDto(
                x.Id,
                x.Number,
                x.EquipmentUnitId,
                x.CustomerId,
                x.OpenedAt,
                x.ProblemDescription,
                x.Kind,
                x.Status,
                x.EstimatedStartAt,
                x.ActualStartAt,
                x.CompletedAt,
                x.ExpectedCompletionAt,
                x.SiteLocation,
                x.JobDescription,
                x.CustomerComplaint,
                x.InternalRemarks,
                x.ResponsibleOfficerName,
                x.ServiceContractId,
                dbContext.ServiceContracts
                    .Where(contract => contract.Id == x.ServiceContractId)
                    .Select(contract => contract.Number)
                    .FirstOrDefault(),
                x.EntitlementSource,
                x.EntitlementCoverage,
                x.CustomerBillingTreatment,
                x.EntitlementEvaluatedAt,
                x.EntitlementSummary))
            .FirstOrDefaultAsync(cancellationToken);

        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("{id:guid}/costing")]
    public async Task<ActionResult<ServiceCostingService.ServiceJobCostingDto>> Costing(Guid id, CancellationToken cancellationToken)
    {
        var costing = await serviceCostingService.GetServiceJobCostingAsync(id, cancellationToken);
        return Ok(costing);
    }

    [HttpGet("{id:guid}/closeout-checks")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobCloseoutCheckDto>>> CloseoutChecks(Guid id, CancellationToken cancellationToken)
    {
        var checks = await serviceManagementService.GetServiceJobCloseoutChecksAsync(id, cancellationToken);
        return Ok(checks.Select(x => new ServiceJobCloseoutCheckDto(x.Key, x.Label, x.IsClear, x.PendingCount, x.Detail)).ToList());
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceJob, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.StartServiceJobAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CompleteServiceJobAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CloseServiceJobAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> Reopen(Guid id, ReopenServiceJobRequest? request, CancellationToken cancellationToken)
    {
        await serviceManagementService.ReopenServiceJobAsync(id, request?.Reason, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/refresh-entitlement")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
    public async Task<ActionResult> RefreshEntitlement(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.RefreshServiceJobEntitlementAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/assignments")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobAssignmentDto>>> Assignments(Guid id, CancellationToken cancellationToken)
    {
        var assignments = await dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => x.ServiceJobId == id)
            .OrderByDescending(x => x.AssignedDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ServiceJobAssignmentDto(
                x.Id,
                x.ServiceJobId,
                x.TechnicianId,
                x.EmployeeName,
                x.Role,
                x.AssignedTask,
                x.AssignedDate,
                x.WorkStartAt,
                x.WorkEndAt,
                x.NormalHours,
                x.OvertimeHours,
                x.DailyWorkDescription,
                x.ApprovalStatus,
                x.ApprovedAt,
                x.RejectedAt,
                x.RejectionReason))
            .ToListAsync(cancellationToken);

        return Ok(assignments);
    }

    [HttpPost("{id:guid}/assignments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult<ServiceJobAssignmentDto>> AddAssignment(Guid id, AddServiceJobAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignmentId = await serviceManagementService.AddServiceJobAssignmentAsync(
            id,
            request.TechnicianId,
            request.EmployeeName,
            request.Role,
            request.AssignedTask,
            request.AssignedDate,
            request.WorkStartAt,
            request.WorkEndAt,
            request.NormalHours,
            request.OvertimeHours,
            request.DailyWorkDescription,
            cancellationToken);

        var assignment = await dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == assignmentId)
            .Select(x => new ServiceJobAssignmentDto(
                x.Id,
                x.ServiceJobId,
                x.TechnicianId,
                x.EmployeeName,
                x.Role,
                x.AssignedTask,
                x.AssignedDate,
                x.WorkStartAt,
                x.WorkEndAt,
                x.NormalHours,
                x.OvertimeHours,
                x.DailyWorkDescription,
                x.ApprovalStatus,
                x.ApprovedAt,
                x.RejectedAt,
                x.RejectionReason))
            .FirstOrDefaultAsync(cancellationToken);

        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpPost("{id:guid}/assignments/{assignmentId:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> ApproveAssignment(Guid id, Guid assignmentId, CancellationToken cancellationToken)
    {
        await serviceManagementService.ApproveServiceJobAssignmentAsync(id, assignmentId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/assignments/{assignmentId:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> RejectAssignment(Guid id, Guid assignmentId, RejectServiceJobAssignmentRequest? request, CancellationToken cancellationToken)
    {
        await serviceManagementService.RejectServiceJobAssignmentAsync(id, assignmentId, request?.Reason, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> RemoveAssignment(Guid id, Guid assignmentId, CancellationToken cancellationToken)
    {
        await serviceManagementService.RemoveServiceJobAssignmentAsync(id, assignmentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/progress-updates")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobProgressUpdateDto>>> ProgressUpdates(Guid id, CancellationToken cancellationToken)
    {
        var updates = await dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => x.ServiceJobId == id)
            .OrderByDescending(x => x.ProgressDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ServiceJobProgressUpdateDto(
                x.Id,
                x.ServiceJobId,
                x.ProgressDate,
                x.WorkCompleted,
                x.WorkPending,
                x.ProblemsFound,
                x.AdditionalPartsRequired,
                x.AdditionalLaborRequired,
                x.CustomerInstructions,
                x.SiteIssues,
                x.TechnicianNotes,
                x.SupervisorNotes,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(updates);
    }

    [HttpPost("{id:guid}/progress-updates")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult<ServiceJobProgressUpdateDto>> AddProgressUpdate(Guid id, AddServiceJobProgressUpdateRequest request, CancellationToken cancellationToken)
    {
        var updateId = await serviceManagementService.AddServiceJobProgressUpdateAsync(
            id,
            request.ProgressDate,
            request.WorkCompleted,
            request.WorkPending,
            request.ProblemsFound,
            request.AdditionalPartsRequired,
            request.AdditionalLaborRequired,
            request.CustomerInstructions,
            request.SiteIssues,
            request.TechnicianNotes,
            request.SupervisorNotes,
            cancellationToken);

        var update = await dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == updateId)
            .Select(x => new ServiceJobProgressUpdateDto(
                x.Id,
                x.ServiceJobId,
                x.ProgressDate,
                x.WorkCompleted,
                x.WorkPending,
                x.ProblemsFound,
                x.AdditionalPartsRequired,
                x.AdditionalLaborRequired,
                x.CustomerInstructions,
                x.SiteIssues,
                x.TechnicianNotes,
                x.SupervisorNotes,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return update is null ? NotFound() : Ok(update);
    }
}

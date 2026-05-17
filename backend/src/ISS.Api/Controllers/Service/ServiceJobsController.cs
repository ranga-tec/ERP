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
        bool FinalInvoiceNotRequired,
        string? FinalInvoiceNotRequiredReason,
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
    public sealed record MarkFinalInvoiceNotRequiredRequest(string Reason);
    public sealed record ServiceJobOperationDto(
        Guid Id,
        Guid ServiceJobId,
        int Sequence,
        string Name,
        string? Description,
        Guid? PlannedItemId,
        string? PlannedItemSku,
        string? PlannedItemName,
        decimal PlannedQuantity,
        decimal EstimatedLaborHours,
        DateTimeOffset? RequiredAt,
        string? Notes,
        ServiceJobOperationStatus Status,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        decimal ActualMaterialQuantity,
        decimal ActualMaterialCost,
        decimal ApprovedLaborHours,
        decimal ApprovedLaborCost);
    public sealed record UpsertServiceJobOperationRequest(
        int Sequence,
        string Name,
        string? Description,
        Guid? PlannedItemId,
        decimal PlannedQuantity,
        decimal EstimatedLaborHours,
        DateTimeOffset? RequiredAt,
        string? Notes);
    public sealed record ServiceJobDailySheetDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        DateTimeOffset SheetDate,
        string PreparedByName,
        string? SiteLocation,
        string? ShiftName,
        string? WeatherOrSiteCondition,
        string WorkPlanned,
        string? WorkCompleted,
        string? WorkPending,
        string? ProblemsFound,
        string? CustomerInstructions,
        string? TechnicianNotes,
        string? SupervisorNotes,
        ServiceJobDailySheetStatus Status,
        DateTimeOffset CreatedAt,
        int AssignmentCount,
        int ProgressCount,
        int MaterialRequisitionCount,
        int MaterialDispositionCount,
        int ExpenseClaimCount,
        int IouCount);
    public sealed record CreateServiceJobDailySheetRequest(
        DateTimeOffset? SheetDate,
        string? PreparedByName,
        string? SiteLocation,
        string? ShiftName,
        string? WeatherOrSiteCondition,
        string WorkPlanned,
        string? WorkCompleted,
        string? WorkPending,
        string? ProblemsFound,
        string? CustomerInstructions,
        string? TechnicianNotes,
        string? SupervisorNotes);
    public sealed record RejectServiceJobDailySheetRequest(string? Reason);
    public sealed record ServiceJobAssignmentDto(
        Guid Id,
        Guid ServiceJobId,
        Guid? ServiceJobDailySheetId,
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
        string? DailyWorkDescription,
        Guid? ServiceJobDailySheetId);
    public sealed record RejectServiceJobAssignmentRequest(string? Reason);
    public sealed record ServiceJobProgressUpdateDto(
        Guid Id,
        Guid ServiceJobId,
        Guid? ServiceJobDailySheetId,
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
        string? SupervisorNotes,
        Guid? ServiceJobDailySheetId);
    public sealed record ServiceJobCloseoutCheckDto(string Key, string Label, bool IsClear, int PendingCount, string Detail);
    public sealed record ServiceJobMaterialDispositionDto(
        Guid Id,
        Guid ServiceJobId,
        Guid? ServiceJobDailySheetId,
        Guid MaterialRequisitionId,
        Guid MaterialRequisitionLineId,
        Guid ItemId,
        Guid WarehouseId,
        ServiceJobMaterialDispositionKind Kind,
        decimal Quantity,
        decimal UnitCost,
        decimal CostImpact,
        string? BatchNumber,
        string Condition,
        string Reason,
        ServiceJobMaterialChargeTo ChargeTo,
        Guid? SupplierReturnId,
        string? ResponsiblePerson,
        IReadOnlyList<string> Serials,
        DateTimeOffset CreatedAt);
    public sealed record AddServiceJobMaterialDispositionRequest(
        Guid MaterialRequisitionLineId,
        ServiceJobMaterialDispositionKind Kind,
        decimal Quantity,
        string? Condition,
        string Reason,
        ServiceJobMaterialChargeTo ChargeTo,
        Guid? SupplierReturnId,
        string? ResponsiblePerson,
        IReadOnlyList<string>? Serials,
        Guid? ServiceJobDailySheetId);

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
                x.FinalInvoiceNotRequired,
                x.FinalInvoiceNotRequiredReason,
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
                x.FinalInvoiceNotRequired,
                x.FinalInvoiceNotRequiredReason,
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

    [HttpPost("{id:guid}/final-invoice-not-required")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> MarkFinalInvoiceNotRequired(Guid id, MarkFinalInvoiceNotRequiredRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.MarkServiceJobFinalInvoiceNotRequiredAsync(id, request.Reason, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/operations")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobOperationDto>>> Operations(Guid id, CancellationToken cancellationToken)
    {
        var materialRows = await (
            from mr in dbContext.MaterialRequisitions.AsNoTracking()
            from line in mr.Lines
            join item in dbContext.Items.AsNoTracking() on line.ItemId equals item.Id
            where mr.ServiceJobId == id && mr.Status == MaterialRequisitionStatus.Posted
            group new { mr, line, item } by line.ItemId into g
            select new
            {
                ItemId = g.Key,
                Quantity = g.Sum(x => x.line.Quantity),
                Cost = g.Sum(x => x.line.Quantity * x.item.DefaultUnitCost)
            })
            .ToDictionaryAsync(x => x.ItemId, x => new { x.Quantity, x.Cost }, cancellationToken);

        var laborTotals = await dbContext.WorkOrders.AsNoTracking()
            .Where(x => x.ServiceJobId == id)
            .SelectMany(x => x.TimeEntries)
            .Where(x => x.Status == WorkOrderTimeEntryStatus.Approved || x.Status == WorkOrderTimeEntryStatus.Invoiced)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Hours = g.Sum(x => x.HoursWorked),
                Cost = g.Sum(x => x.HoursWorked * x.CostRate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var operations = await (
            from operation in dbContext.ServiceJobOperations.AsNoTracking()
            join item in dbContext.Items.AsNoTracking() on operation.PlannedItemId equals item.Id into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            where operation.ServiceJobId == id
            orderby operation.Sequence, operation.CreatedAt
            select new
            {
                Operation = operation,
                PlannedItemSku = item == null ? null : item.Sku,
                PlannedItemName = item == null ? null : item.Name
            })
            .ToListAsync(cancellationToken);

        var plannedLaborTotal = operations.Sum(x => x.Operation.EstimatedLaborHours);
        var plannedQuantityByItem = operations
            .Where(x => x.Operation.PlannedItemId is not null)
            .GroupBy(x => x.Operation.PlannedItemId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(row => row.Operation.PlannedQuantity));
        var rows = operations.Select(x =>
        {
            var material = x.Operation.PlannedItemId is { } plannedItemId && materialRows.TryGetValue(plannedItemId, out var match)
                ? match
                : null;
            var materialShare = x.Operation.PlannedItemId is { } itemId
                                && plannedQuantityByItem.TryGetValue(itemId, out var plannedItemQuantity)
                                && plannedItemQuantity > 0m
                                && x.Operation.PlannedQuantity > 0m
                ? x.Operation.PlannedQuantity / plannedItemQuantity
                : material is null ? 0m : 1m;
            var laborShare = plannedLaborTotal > 0m && x.Operation.EstimatedLaborHours > 0m
                ? x.Operation.EstimatedLaborHours / plannedLaborTotal
                : 0m;

            return new ServiceJobOperationDto(
                x.Operation.Id,
                x.Operation.ServiceJobId,
                x.Operation.Sequence,
                x.Operation.Name,
                x.Operation.Description,
                x.Operation.PlannedItemId,
                x.PlannedItemSku,
                x.PlannedItemName,
                x.Operation.PlannedQuantity,
                x.Operation.EstimatedLaborHours,
                x.Operation.RequiredAt,
                x.Operation.Notes,
                x.Operation.Status,
                x.Operation.StartedAt,
                x.Operation.CompletedAt,
                material is null ? 0m : material.Quantity * materialShare,
                material is null ? 0m : material.Cost * materialShare,
                laborTotals is null ? 0m : laborTotals.Hours * laborShare,
                laborTotals is null ? 0m : laborTotals.Cost * laborShare);
        }).ToList();

        return Ok(rows);
    }

    [HttpPost("{id:guid}/operations")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult<object>> AddOperation(Guid id, UpsertServiceJobOperationRequest request, CancellationToken cancellationToken)
    {
        var operationId = await serviceManagementService.AddServiceJobOperationAsync(
            id,
            request.Sequence,
            request.Name,
            request.Description,
            request.PlannedItemId,
            request.PlannedQuantity,
            request.EstimatedLaborHours,
            request.RequiredAt,
            request.Notes,
            cancellationToken);

        return Ok(new { id = operationId });
    }

    [HttpPut("{id:guid}/operations/{operationId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> UpdateOperation(Guid id, Guid operationId, UpsertServiceJobOperationRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceJobOperationAsync(
            id,
            operationId,
            request.Sequence,
            request.Name,
            request.Description,
            request.PlannedItemId,
            request.PlannedQuantity,
            request.EstimatedLaborHours,
            request.RequiredAt,
            request.Notes,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/operations/{operationId:guid}/start")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> StartOperation(Guid id, Guid operationId, CancellationToken cancellationToken)
    {
        await serviceManagementService.StartServiceJobOperationAsync(id, operationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/operations/{operationId:guid}/complete")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> CompleteOperation(Guid id, Guid operationId, CancellationToken cancellationToken)
    {
        await serviceManagementService.CompleteServiceJobOperationAsync(id, operationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/operations/{operationId:guid}/skip")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> SkipOperation(Guid id, Guid operationId, CancellationToken cancellationToken)
    {
        await serviceManagementService.SkipServiceJobOperationAsync(id, operationId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/operations/{operationId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> RemoveOperation(Guid id, Guid operationId, CancellationToken cancellationToken)
    {
        await serviceManagementService.RemoveServiceJobOperationAsync(id, operationId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/daily-sheets")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobDailySheetDto>>> DailySheets(Guid id, CancellationToken cancellationToken)
    {
        var sheets = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => x.ServiceJobId == id)
            .OrderByDescending(x => x.SheetDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ServiceJobDailySheetDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.SheetDate,
                x.PreparedByName,
                x.SiteLocation,
                x.ShiftName,
                x.WeatherOrSiteCondition,
                x.WorkPlanned,
                x.WorkCompleted,
                x.WorkPending,
                x.ProblemsFound,
                x.CustomerInstructions,
                x.TechnicianNotes,
                x.SupervisorNotes,
                x.Status,
                x.CreatedAt,
                dbContext.ServiceJobAssignments.Count(a => a.ServiceJobDailySheetId == x.Id),
                dbContext.ServiceJobProgressUpdates.Count(p => p.ServiceJobDailySheetId == x.Id),
                dbContext.MaterialRequisitions.Count(m => m.ServiceJobDailySheetId == x.Id),
                dbContext.ServiceJobMaterialDispositions.Count(d => d.ServiceJobDailySheetId == x.Id),
                dbContext.ServiceExpenseClaims.Count(c => c.ServiceJobDailySheetId == x.Id),
                dbContext.PettyCashIous.Count(i => i.ServiceJobDailySheetId == x.Id)))
            .ToListAsync(cancellationToken);

        return Ok(sheets);
    }

    [HttpPost("{id:guid}/daily-sheets")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult<ServiceJobDailySheetDto>> CreateDailySheet(Guid id, CreateServiceJobDailySheetRequest request, CancellationToken cancellationToken)
    {
        var preparedByName = string.IsNullOrWhiteSpace(request.PreparedByName)
            ? User.Identity?.Name ?? "Unknown user"
            : request.PreparedByName;
        var dailySheetId = await serviceManagementService.CreateServiceJobDailySheetAsync(
            id,
            request.SheetDate,
            preparedByName,
            request.SiteLocation,
            request.ShiftName,
            request.WeatherOrSiteCondition,
            request.WorkPlanned,
            request.WorkCompleted,
            request.WorkPending,
            request.ProblemsFound,
            request.CustomerInstructions,
            request.TechnicianNotes,
            request.SupervisorNotes,
            cancellationToken);

        var sheet = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == dailySheetId)
            .Select(x => new ServiceJobDailySheetDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.SheetDate,
                x.PreparedByName,
                x.SiteLocation,
                x.ShiftName,
                x.WeatherOrSiteCondition,
                x.WorkPlanned,
                x.WorkCompleted,
                x.WorkPending,
                x.ProblemsFound,
                x.CustomerInstructions,
                x.TechnicianNotes,
                x.SupervisorNotes,
                x.Status,
                x.CreatedAt,
                0,
                0,
                0,
                0,
                0,
                0))
            .FirstOrDefaultAsync(cancellationToken);
        return sheet is null ? NotFound() : Ok(sheet);
    }

    [HttpPost("{id:guid}/daily-sheets/{dailySheetId:guid}/submit")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> SubmitDailySheet(Guid id, Guid dailySheetId, CancellationToken cancellationToken)
    {
        await serviceManagementService.SubmitServiceJobDailySheetAsync(id, dailySheetId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/daily-sheets/{dailySheetId:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> ApproveDailySheet(Guid id, Guid dailySheetId, CancellationToken cancellationToken)
    {
        await serviceManagementService.ApproveServiceJobDailySheetAsync(id, dailySheetId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/daily-sheets/{dailySheetId:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
    public async Task<ActionResult> RejectDailySheet(Guid id, Guid dailySheetId, RejectServiceJobDailySheetRequest? request, CancellationToken cancellationToken)
    {
        await serviceManagementService.RejectServiceJobDailySheetAsync(id, dailySheetId, request?.Reason, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/material-dispositions")]
    public async Task<ActionResult<IReadOnlyList<ServiceJobMaterialDispositionDto>>> MaterialDispositions(Guid id, CancellationToken cancellationToken)
    {
        var rows = await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
            .Where(x => x.ServiceJobId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ServiceJobMaterialDispositionDto(
                x.Id,
                x.ServiceJobId,
                x.ServiceJobDailySheetId,
                x.MaterialRequisitionId,
                x.MaterialRequisitionLineId,
                x.ItemId,
                x.WarehouseId,
                x.Kind,
                x.Quantity,
                x.UnitCost,
                x.CostImpact,
                x.BatchNumber,
                x.Condition,
                x.Reason,
                x.ChargeTo,
                x.SupplierReturnId,
                x.ResponsiblePerson,
                x.Serials.Select(s => s.SerialNumber).ToList(),
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("{id:guid}/material-dispositions")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Inventory}")]
    public async Task<ActionResult<ServiceJobMaterialDispositionDto>> AddMaterialDisposition(
        Guid id,
        AddServiceJobMaterialDispositionRequest request,
        CancellationToken cancellationToken)
    {
        var dispositionId = await serviceManagementService.AddServiceJobMaterialDispositionAsync(
            id,
            request.MaterialRequisitionLineId,
            request.Kind,
            request.Quantity,
            request.Condition,
            request.Reason,
            request.ChargeTo,
            request.SupplierReturnId,
            request.ResponsiblePerson,
            request.Serials,
            request.ServiceJobDailySheetId,
            cancellationToken);

        var disposition = await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == dispositionId)
            .Select(x => new ServiceJobMaterialDispositionDto(
                x.Id,
                x.ServiceJobId,
                x.ServiceJobDailySheetId,
                x.MaterialRequisitionId,
                x.MaterialRequisitionLineId,
                x.ItemId,
                x.WarehouseId,
                x.Kind,
                x.Quantity,
                x.UnitCost,
                x.CostImpact,
                x.BatchNumber,
                x.Condition,
                x.Reason,
                x.ChargeTo,
                x.SupplierReturnId,
                x.ResponsiblePerson,
                x.Serials.Select(s => s.SerialNumber).ToList(),
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return disposition is null ? NotFound() : Ok(disposition);
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
                x.ServiceJobDailySheetId,
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
            request.ServiceJobDailySheetId,
            cancellationToken);

        var assignment = await dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == assignmentId)
            .Select(x => new ServiceJobAssignmentDto(
                x.Id,
                x.ServiceJobId,
                x.ServiceJobDailySheetId,
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
                x.ServiceJobDailySheetId,
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
            request.ServiceJobDailySheetId,
            cancellationToken);

        var update = await dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => x.ServiceJobId == id && x.Id == updateId)
            .Select(x => new ServiceJobProgressUpdateDto(
                x.Id,
                x.ServiceJobId,
                x.ServiceJobDailySheetId,
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

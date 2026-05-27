using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
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
        ServiceJobMaterialDispositionStatus Status,
        DateTimeOffset? PostedAt,
        bool IsVoided,
        DateTimeOffset? VoidedAt,
        string? VoidReason,
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
    public sealed record UpdateServiceJobMaterialDispositionRequest(
        string? Condition,
        string Reason,
        ServiceJobMaterialChargeTo ChargeTo,
        Guid? SupplierReturnId,
        string? ResponsiblePerson);
    public sealed record VoidServiceJobMaterialDispositionRequest(string? Reason);
    public sealed record ServiceDashboardMetricDto(string Key, string Label, int Count);
    public sealed record ServiceDashboardQueueDto(
        int ActiveJobs,
        int OverdueJobs,
        int JobsWithoutDailySheetToday,
        int JobsWithoutProgressToday,
        int PendingDailySheets,
        int PendingIous,
        int PendingExpenseClaims,
        int PendingMaterialRequests,
        int PendingMaterialDispositions,
        int CompletedAwaitingServiceTaken,
        int ServiceTakenAwaitingInvoice);
    public sealed record ServiceDashboardJobCardDto(
        Guid Id,
        string Number,
        string CustomerCode,
        string CustomerName,
        string EquipmentSerialNumber,
        ServiceJobKind Kind,
        ServiceJobStatus Status,
        DateTimeOffset OpenedAt,
        DateTimeOffset? ExpectedCompletionAt,
        string? ResponsibleOfficerName,
        DateTimeOffset? LatestProgressAt,
        int StaffToday,
        int PendingDailySheets,
        int PendingIous,
        int PendingExpenseClaims,
        int PendingMaterialRequests,
        int PendingMaterialDispositions,
        bool HasCompletedServiceTaken,
        int PendingBlockers,
        string NextAction,
        string NextActionHref);
    public sealed record ServiceDashboardDto(
        DateTimeOffset GeneratedAt,
        IReadOnlyList<ServiceDashboardMetricDto> StatusCounts,
        IReadOnlyList<ServiceDashboardMetricDto> StageCounts,
        ServiceDashboardQueueDto Queues,
        IReadOnlyList<ServiceDashboardJobCardDto> ActiveJobs,
        IReadOnlyList<ServiceDashboardJobCardDto> FinancialQueue,
        IReadOnlyList<ServiceDashboardJobCardDto> BillingQueue);
    public sealed record ServiceDispatchJobDto(
        Guid Id,
        string Number,
        string CustomerCode,
        string CustomerName,
        string EquipmentSerialNumber,
        ServiceJobKind Kind,
        ServiceJobStatus Status,
        DateTimeOffset OpenedAt,
        DateTimeOffset? ExpectedCompletionAt,
        string? ResponsibleOfficerName,
        IReadOnlyList<string> AssignedStaff,
        DateTimeOffset? LatestProgressAt,
        bool HasDailySheetToday,
        int PendingDailySheets,
        string NextAction,
        string NextActionHref);
    public sealed record ServiceDispatchBoardDto(
        DateTimeOffset GeneratedAt,
        IReadOnlyList<ServiceDispatchJobDto> UnassignedJobs,
        IReadOnlyList<ServiceDispatchJobDto> AssignedJobs,
        IReadOnlyList<ServiceDispatchJobDto> WaitingJobs,
        IReadOnlyList<ServiceDispatchJobDto> CompletedJobs);
    public sealed record ServiceTechnicianAssignmentDto(
        Guid AssignmentId,
        Guid ServiceJobId,
        string JobNumber,
        string CustomerCode,
        string EquipmentSerialNumber,
        string EmployeeName,
        string Role,
        string AssignedTask,
        DateTimeOffset AssignedDate,
        DateTimeOffset? WorkStartAt,
        DateTimeOffset? WorkEndAt,
        decimal NormalHours,
        decimal OvertimeHours,
        ServiceJobAssignmentApprovalStatus ApprovalStatus,
        Guid? DailySheetId,
        string? DailySheetNumber,
        ServiceJobDailySheetStatus? DailySheetStatus,
        string JobHref,
        string ProgressHref,
        string MaterialHref,
        string IouHref,
        string ExpenseHref);
    public sealed record ServiceTechnicianDailySheetDto(
        Guid Id,
        Guid ServiceJobId,
        string Number,
        string JobNumber,
        string CustomerCode,
        string EquipmentSerialNumber,
        DateTimeOffset SheetDate,
        string PreparedByName,
        string WorkPlanned,
        string? WorkCompleted,
        string? WorkPending,
        ServiceJobDailySheetStatus Status,
        int AssignmentCount,
        int ProgressCount,
        string DailySheetHref,
        string ProgressHref,
        string MaterialHref,
        string IouHref,
        string ExpenseHref);
    public sealed record ServiceTechnicianWorkbenchDto(
        DateTimeOffset GeneratedAt,
        IReadOnlyList<ServiceTechnicianAssignmentDto> TodayAssignments,
        IReadOnlyList<ServiceTechnicianDailySheetDto> OpenDailySheets,
        IReadOnlyList<ServiceDispatchJobDto> ActiveJobs);

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

    [HttpGet("dashboard")]
    public async Task<ActionResult<ServiceDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var activeStatuses = new[]
        {
            ServiceJobStatus.Draft,
            ServiceJobStatus.Open,
            ServiceJobStatus.Assigned,
            ServiceJobStatus.InProgress,
            ServiceJobStatus.WaitingForParts,
            ServiceJobStatus.WaitingForCustomerApproval,
            ServiceJobStatus.WaitingForSupplier,
            ServiceJobStatus.WorkCompleted,
            ServiceJobStatus.PendingExpenseSettlement,
            ServiceJobStatus.PendingMaterialReturn,
            ServiceJobStatus.ReadyForInvoice,
            ServiceJobStatus.Invoiced,
            ServiceJobStatus.Reopened
        };

        var statusCounts = await dbContext.ServiceJobs.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var stageCounts = statusCounts
            .GroupBy(x => ServiceStageKey(x.Status))
            .Select(x => new ServiceDashboardMetricDto(x.Key, ServiceStageLabel(x.Key), x.Sum(y => y.Count)))
            .OrderBy(x => ServiceStageSort(x.Key))
            .ToList();

        var statusMetrics = statusCounts
            .OrderBy(x => (int)x.Status)
            .Select(x => new ServiceDashboardMetricDto(((int)x.Status).ToString(), ServiceJobStatusLabel(x.Status), x.Count))
            .ToList();

        var activeJobsRaw = await (
            from job in dbContext.ServiceJobs.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking() on job.CustomerId equals customer.Id
            join unit in dbContext.EquipmentUnits.AsNoTracking() on job.EquipmentUnitId equals unit.Id
            where activeStatuses.Contains(job.Status)
            orderby job.ExpectedCompletionAt ?? job.OpenedAt, job.OpenedAt
            select new
            {
                job.Id,
                job.Number,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                EquipmentSerialNumber = unit.SerialNumber,
                job.Kind,
                job.Status,
                job.OpenedAt,
                job.ExpectedCompletionAt,
                job.ResponsibleOfficerName,
                job.FinalInvoiceNotRequired
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        var activeJobIds = activeJobsRaw.Select(x => x.Id).ToList();
        var pendingDailySheetCounts = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && (x.Status == ServiceJobDailySheetStatus.Draft || x.Status == ServiceJobDailySheetStatus.Submitted))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var latestProgress = await dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Latest = x.Max(y => y.ProgressDate) })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Latest, cancellationToken);
        var staffTodayCounts = await dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && x.AssignedDate >= today)
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var pendingIouCounts = await dbContext.PettyCashIous.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && (x.Status == PettyCashIouStatus.Submitted || x.Status == PettyCashIouStatus.Approved || x.Status == PettyCashIouStatus.Released))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var pendingClaimCounts = await dbContext.ServiceExpenseClaims.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && (x.Status == ServiceExpenseClaimStatus.Submitted || x.Status == ServiceExpenseClaimStatus.Approved))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var pendingMrnCounts = await dbContext.MaterialRequisitions.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && x.Status == MaterialRequisitionStatus.Draft)
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var pendingDispositionCounts = await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && x.Status == ServiceJobMaterialDispositionStatus.Draft && !x.IsVoided)
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var completedHandoverJobIds = await dbContext.ServiceHandovers.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && x.Status == ServiceHandoverStatus.Completed)
            .Select(x => x.ServiceJobId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var jobsWithDailySheetToday = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => activeJobIds.Contains(x.ServiceJobId) && x.SheetDate >= today)
            .Select(x => x.ServiceJobId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var completedHandoverSet = completedHandoverJobIds.ToHashSet();
        var dailySheetTodaySet = jobsWithDailySheetToday.ToHashSet();
        var cards = activeJobsRaw.Select(job =>
        {
            var pendingDailySheets = pendingDailySheetCounts.GetValueOrDefault(job.Id);
            var pendingIous = pendingIouCounts.GetValueOrDefault(job.Id);
            var pendingClaims = pendingClaimCounts.GetValueOrDefault(job.Id);
            var pendingMrns = pendingMrnCounts.GetValueOrDefault(job.Id);
            var pendingDispositions = pendingDispositionCounts.GetValueOrDefault(job.Id);
            latestProgress.TryGetValue(job.Id, out var latestProgressAt);
            var hasServiceTaken = completedHandoverSet.Contains(job.Id);
            var pendingBlockers = pendingDailySheets + pendingIous + pendingClaims + pendingMrns + pendingDispositions;
            var nextAction = ServiceDashboardNextAction(job.Id, job.Status, job.FinalInvoiceNotRequired, hasServiceTaken, pendingDailySheets, pendingIous, pendingClaims, pendingMrns, pendingDispositions);

            return new ServiceDashboardJobCardDto(
                job.Id,
                job.Number,
                job.CustomerCode,
                job.CustomerName,
                job.EquipmentSerialNumber,
                job.Kind,
                job.Status,
                job.OpenedAt,
                job.ExpectedCompletionAt,
                job.ResponsibleOfficerName,
                latestProgressAt == default ? null : latestProgressAt,
                staffTodayCounts.GetValueOrDefault(job.Id),
                pendingDailySheets,
                pendingIous,
                pendingClaims,
                pendingMrns,
                pendingDispositions,
                hasServiceTaken,
                pendingBlockers,
                nextAction.Label,
                nextAction.Href);
        }).ToList();

        var queues = new ServiceDashboardQueueDto(
            cards.Count,
            cards.Count(x => x.ExpectedCompletionAt.HasValue && x.ExpectedCompletionAt.Value < now && x.Status != ServiceJobStatus.Closed && x.Status != ServiceJobStatus.Cancelled),
            cards.Count(x => !dailySheetTodaySet.Contains(x.Id) && x.Status is ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.InProgress or ServiceJobStatus.Reopened),
            cards.Count(x => x.LatestProgressAt is null || x.LatestProgressAt < today),
            cards.Sum(x => x.PendingDailySheets),
            cards.Sum(x => x.PendingIous),
            cards.Sum(x => x.PendingExpenseClaims),
            cards.Sum(x => x.PendingMaterialRequests),
            cards.Sum(x => x.PendingMaterialDispositions),
            cards.Count(x => x.Status == ServiceJobStatus.WorkCompleted && !x.HasCompletedServiceTaken),
            cards.Count(x => x.HasCompletedServiceTaken && x.Status is ServiceJobStatus.WorkCompleted or ServiceJobStatus.ReadyForInvoice));

        return Ok(new ServiceDashboardDto(
            now,
            statusMetrics,
            stageCounts,
            queues,
            cards.Take(30).ToList(),
            cards.Where(x => x.PendingIous + x.PendingExpenseClaims > 0).Take(20).ToList(),
            cards.Where(x => x.Status is ServiceJobStatus.WorkCompleted or ServiceJobStatus.ReadyForInvoice || x.HasCompletedServiceTaken).Take(20).ToList()));
    }

    [HttpGet("dispatch-board")]
    public async Task<ActionResult<ServiceDispatchBoardDto>> DispatchBoard(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var dispatchJobs = await BuildDispatchJobsAsync(today, 200, cancellationToken);

        return Ok(new ServiceDispatchBoardDto(
            now,
            dispatchJobs
                .Where(x => x.AssignedStaff.Count == 0 && x.Status is ServiceJobStatus.Open or ServiceJobStatus.Reopened)
                .Take(40)
                .ToList(),
            dispatchJobs
                .Where(x => x.AssignedStaff.Count > 0 && x.Status is ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.InProgress or ServiceJobStatus.Reopened)
                .Take(60)
                .ToList(),
            dispatchJobs
                .Where(x => x.Status is ServiceJobStatus.WaitingForParts or ServiceJobStatus.WaitingForCustomerApproval or ServiceJobStatus.WaitingForSupplier or ServiceJobStatus.PendingExpenseSettlement or ServiceJobStatus.PendingMaterialReturn)
                .Take(40)
                .ToList(),
            dispatchJobs
                .Where(x => x.Status is ServiceJobStatus.WorkCompleted or ServiceJobStatus.ReadyForInvoice or ServiceJobStatus.Invoiced)
                .Take(40)
                .ToList()));
    }

    [HttpGet("technician-workbench")]
    public async Task<ActionResult<ServiceTechnicianWorkbenchDto>> TechnicianWorkbench(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var tomorrow = today.AddDays(1);

        var todayAssignments = await (
            from assignment in dbContext.ServiceJobAssignments.AsNoTracking()
            join job in dbContext.ServiceJobs.AsNoTracking() on assignment.ServiceJobId equals job.Id
            join customer in dbContext.Customers.AsNoTracking() on job.CustomerId equals customer.Id
            join unit in dbContext.EquipmentUnits.AsNoTracking() on job.EquipmentUnitId equals unit.Id
            join sheet in dbContext.ServiceJobDailySheets.AsNoTracking() on assignment.ServiceJobDailySheetId equals sheet.Id into sheets
            from dailySheet in sheets.DefaultIfEmpty()
            where assignment.AssignedDate >= today && assignment.AssignedDate < tomorrow && job.Status != ServiceJobStatus.Closed && job.Status != ServiceJobStatus.Cancelled
            orderby assignment.AssignedDate, assignment.EmployeeName
            select new ServiceTechnicianAssignmentDto(
                assignment.Id,
                job.Id,
                job.Number,
                customer.Code,
                unit.SerialNumber,
                assignment.EmployeeName,
                assignment.Role,
                assignment.AssignedTask,
                assignment.AssignedDate,
                assignment.WorkStartAt,
                assignment.WorkEndAt,
                assignment.NormalHours,
                assignment.OvertimeHours,
                assignment.ApprovalStatus,
                dailySheet == null ? null : dailySheet.Id,
                dailySheet == null ? null : dailySheet.Number,
                dailySheet == null ? null : dailySheet.Status,
                $"/service/jobs/{job.Id}",
                $"/service/jobs/{job.Id}?tab=daily-work&dailyView=progress{(dailySheet == null ? string.Empty : $"&dailySheetId={dailySheet.Id}")}",
                $"/service/jobs/{job.Id}?tab=materials&materialView=issues",
                $"/service/jobs/{job.Id}?tab=expenses&expenseView=ious",
                $"/service/jobs/{job.Id}?tab=expenses&expenseView=reimbursements"))
            .Take(100)
            .ToListAsync(cancellationToken);

        var openDailySheets = await (
            from sheet in dbContext.ServiceJobDailySheets.AsNoTracking()
            join job in dbContext.ServiceJobs.AsNoTracking() on sheet.ServiceJobId equals job.Id
            join customer in dbContext.Customers.AsNoTracking() on job.CustomerId equals customer.Id
            join unit in dbContext.EquipmentUnits.AsNoTracking() on job.EquipmentUnitId equals unit.Id
            where sheet.Status != ServiceJobDailySheetStatus.Approved && job.Status != ServiceJobStatus.Closed && job.Status != ServiceJobStatus.Cancelled
            orderby sheet.SheetDate descending
            select new ServiceTechnicianDailySheetDto(
                sheet.Id,
                job.Id,
                sheet.Number,
                job.Number,
                customer.Code,
                unit.SerialNumber,
                sheet.SheetDate,
                sheet.PreparedByName,
                sheet.WorkPlanned,
                sheet.WorkCompleted,
                sheet.WorkPending,
                sheet.Status,
                dbContext.ServiceJobAssignments.Count(x => x.ServiceJobDailySheetId == sheet.Id),
                dbContext.ServiceJobProgressUpdates.Count(x => x.ServiceJobDailySheetId == sheet.Id),
                $"/service/jobs/{job.Id}?tab=daily-work&dailyView=sheets&dailySheetId={sheet.Id}",
                $"/service/jobs/{job.Id}?tab=daily-work&dailyView=progress&dailySheetId={sheet.Id}",
                $"/service/jobs/{job.Id}?tab=materials&materialView=issues",
                $"/service/jobs/{job.Id}?tab=expenses&expenseView=ious",
                $"/service/jobs/{job.Id}?tab=expenses&expenseView=reimbursements"))
            .Take(80)
            .ToListAsync(cancellationToken);

        var activeJobs = await BuildDispatchJobsAsync(today, 80, cancellationToken);

        return Ok(new ServiceTechnicianWorkbenchDto(
            now,
            todayAssignments,
            openDailySheets,
            activeJobs
                .Where(x => x.Status is ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.InProgress or ServiceJobStatus.Reopened)
                .Take(30)
                .ToList()));
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
                dbContext.ServiceJobMaterialDispositions.Count(d => d.ServiceJobDailySheetId == x.Id && !d.IsVoided),
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
                x.Status,
                x.PostedAt,
                x.IsVoided,
                x.VoidedAt,
                x.VoidReason,
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
                x.Status,
                x.PostedAt,
                x.IsVoided,
                x.VoidedAt,
                x.VoidReason,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return disposition is null ? NotFound() : Ok(disposition);
    }

    [HttpPost("{id:guid}/material-dispositions/{dispositionId:guid}/post")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Inventory}")]
    public async Task<ActionResult> PostMaterialDisposition(
        Guid id,
        Guid dispositionId,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.PostServiceJobMaterialDispositionAsync(id, dispositionId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/material-dispositions/{dispositionId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Inventory}")]
    public async Task<ActionResult> UpdateMaterialDisposition(
        Guid id,
        Guid dispositionId,
        UpdateServiceJobMaterialDispositionRequest request,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceJobMaterialDispositionAsync(
            id,
            dispositionId,
            request.Condition,
            request.Reason,
            request.ChargeTo,
            request.SupplierReturnId,
            request.ResponsiblePerson,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/material-dispositions/{dispositionId:guid}/void")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Inventory}")]
    public async Task<ActionResult> VoidMaterialDisposition(
        Guid id,
        Guid dispositionId,
        VoidServiceJobMaterialDispositionRequest? request,
        CancellationToken cancellationToken)
    {
        await serviceManagementService.VoidServiceJobMaterialDispositionAsync(
            id,
            dispositionId,
            request?.Reason,
            cancellationToken);
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

    private static string ServiceJobStatusLabel(ServiceJobStatus status) => status switch
    {
        ServiceJobStatus.Draft => "Draft",
        ServiceJobStatus.Open => "Open",
        ServiceJobStatus.Assigned => "Assigned",
        ServiceJobStatus.InProgress => "In Progress",
        ServiceJobStatus.WaitingForParts => "Waiting for Parts",
        ServiceJobStatus.WaitingForCustomerApproval => "Waiting for Customer Approval",
        ServiceJobStatus.WaitingForSupplier => "Waiting for Supplier",
        ServiceJobStatus.WorkCompleted => "Work Completed",
        ServiceJobStatus.PendingExpenseSettlement => "Pending Expense Settlement",
        ServiceJobStatus.PendingMaterialReturn => "Pending Material Return",
        ServiceJobStatus.ReadyForInvoice => "Ready for Invoice",
        ServiceJobStatus.Invoiced => "Invoiced",
        ServiceJobStatus.Closed => "Closed",
        ServiceJobStatus.Reopened => "Reopened",
        ServiceJobStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string ServiceStageKey(ServiceJobStatus status) => status switch
    {
        ServiceJobStatus.Draft or ServiceJobStatus.Open or ServiceJobStatus.Reopened => "intake",
        ServiceJobStatus.Assigned => "plan",
        ServiceJobStatus.InProgress => "daily-work",
        ServiceJobStatus.WaitingForParts or ServiceJobStatus.WaitingForCustomerApproval or ServiceJobStatus.WaitingForSupplier => "waiting",
        ServiceJobStatus.WorkCompleted or ServiceJobStatus.PendingExpenseSettlement or ServiceJobStatus.PendingMaterialReturn or ServiceJobStatus.ReadyForInvoice => "closeout",
        ServiceJobStatus.Invoiced => "invoiced",
        ServiceJobStatus.Closed => "closed",
        ServiceJobStatus.Cancelled => "cancelled",
        _ => "other"
    };

    private static string ServiceStageLabel(string key) => key switch
    {
        "intake" => "Intake",
        "plan" => "Plan",
        "daily-work" => "Daily Work",
        "waiting" => "Waiting",
        "closeout" => "Closeout",
        "invoiced" => "Invoiced",
        "closed" => "Closed",
        "cancelled" => "Cancelled",
        _ => "Other"
    };

    private static int ServiceStageSort(string key) => key switch
    {
        "intake" => 0,
        "plan" => 1,
        "daily-work" => 2,
        "waiting" => 3,
        "closeout" => 4,
        "invoiced" => 5,
        "closed" => 6,
        "cancelled" => 7,
        _ => 99
    };

    private async Task<IReadOnlyList<ServiceDispatchJobDto>> BuildDispatchJobsAsync(DateTimeOffset today, int take, CancellationToken cancellationToken)
    {
        var activeStatuses = new[]
        {
            ServiceJobStatus.Open,
            ServiceJobStatus.Assigned,
            ServiceJobStatus.InProgress,
            ServiceJobStatus.WaitingForParts,
            ServiceJobStatus.WaitingForCustomerApproval,
            ServiceJobStatus.WaitingForSupplier,
            ServiceJobStatus.WorkCompleted,
            ServiceJobStatus.PendingExpenseSettlement,
            ServiceJobStatus.PendingMaterialReturn,
            ServiceJobStatus.ReadyForInvoice,
            ServiceJobStatus.Invoiced,
            ServiceJobStatus.Reopened
        };

        var jobs = await (
            from job in dbContext.ServiceJobs.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking() on job.CustomerId equals customer.Id
            join unit in dbContext.EquipmentUnits.AsNoTracking() on job.EquipmentUnitId equals unit.Id
            where activeStatuses.Contains(job.Status)
            orderby job.ExpectedCompletionAt ?? job.OpenedAt, job.OpenedAt
            select new
            {
                job.Id,
                job.Number,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                EquipmentSerialNumber = unit.SerialNumber,
                job.Kind,
                job.Status,
                job.OpenedAt,
                job.ExpectedCompletionAt,
                job.ResponsibleOfficerName
            })
            .Take(take)
            .ToListAsync(cancellationToken);

        var jobIds = jobs.Select(x => x.Id).ToList();
        var assignedStaff = await dbContext.ServiceJobAssignments.AsNoTracking()
            .Where(x => jobIds.Contains(x.ServiceJobId) && x.ApprovalStatus != ServiceJobAssignmentApprovalStatus.Rejected)
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new
            {
                ServiceJobId = x.Key,
                Staff = x
                    .OrderByDescending(y => y.AssignedDate)
                    .Select(y => y.EmployeeName)
                    .Distinct()
                    .Take(4)
                    .ToList()
            })
            .ToDictionaryAsync(x => x.ServiceJobId, x => (IReadOnlyList<string>)x.Staff, cancellationToken);
        var latestProgress = await dbContext.ServiceJobProgressUpdates.AsNoTracking()
            .Where(x => jobIds.Contains(x.ServiceJobId))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Latest = x.Max(y => y.ProgressDate) })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Latest, cancellationToken);
        var dailySheetToday = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => jobIds.Contains(x.ServiceJobId) && x.SheetDate >= today)
            .Select(x => x.ServiceJobId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pendingDailySheets = await dbContext.ServiceJobDailySheets.AsNoTracking()
            .Where(x => jobIds.Contains(x.ServiceJobId) && (x.Status == ServiceJobDailySheetStatus.Draft || x.Status == ServiceJobDailySheetStatus.Submitted))
            .GroupBy(x => x.ServiceJobId)
            .Select(x => new { ServiceJobId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ServiceJobId, x => x.Count, cancellationToken);
        var dailySheetTodaySet = dailySheetToday.ToHashSet();

        return jobs.Select(job =>
        {
            assignedStaff.TryGetValue(job.Id, out var staff);
            latestProgress.TryGetValue(job.Id, out var latestProgressAt);
            var nextAction = ServiceDispatchNextAction(job.Id, job.Status, staff?.Count ?? 0, dailySheetTodaySet.Contains(job.Id), pendingDailySheets.GetValueOrDefault(job.Id));

            return new ServiceDispatchJobDto(
                job.Id,
                job.Number,
                job.CustomerCode,
                job.CustomerName,
                job.EquipmentSerialNumber,
                job.Kind,
                job.Status,
                job.OpenedAt,
                job.ExpectedCompletionAt,
                job.ResponsibleOfficerName,
                staff ?? Array.Empty<string>(),
                latestProgressAt == default ? null : latestProgressAt,
                dailySheetTodaySet.Contains(job.Id),
                pendingDailySheets.GetValueOrDefault(job.Id),
                nextAction.Label,
                nextAction.Href);
        }).ToList();
    }

    private static (string Label, string Href) ServiceDispatchNextAction(Guid serviceJobId, ServiceJobStatus status, int assignedStaffCount, bool hasDailySheetToday, int pendingDailySheets)
    {
        var jobHref = $"/service/jobs/{serviceJobId}";

        if (assignedStaffCount == 0 && status is ServiceJobStatus.Open or ServiceJobStatus.Reopened)
        {
            return ("Assign staff", $"{jobHref}?tab=daily-work&dailyView=labor");
        }

        if (!hasDailySheetToday && status is ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.InProgress or ServiceJobStatus.Reopened)
        {
            return ("Create daily sheet", $"{jobHref}?tab=daily-work&dailyView=sheets");
        }

        if (pendingDailySheets > 0)
        {
            return ("Review daily sheets", $"{jobHref}?tab=daily-work&dailyView=sheets");
        }

        if (status == ServiceJobStatus.WorkCompleted)
        {
            return ("Prepare closeout", $"{jobHref}?tab=billing");
        }

        return ("Open job", jobHref);
    }

    private static (string Label, string Href) ServiceDashboardNextAction(
        Guid serviceJobId,
        ServiceJobStatus status,
        bool finalInvoiceNotRequired,
        bool hasCompletedServiceTaken,
        int pendingDailySheets,
        int pendingIous,
        int pendingClaims,
        int pendingMrns,
        int pendingDispositions)
    {
        var jobHref = $"/service/jobs/{serviceJobId}";

        if (status is ServiceJobStatus.Open or ServiceJobStatus.Assigned or ServiceJobStatus.Reopened)
        {
            return ("Start or update daily work", $"{jobHref}?tab=daily-work&dailyView=sheets");
        }

        if (pendingDailySheets > 0)
        {
            return ("Review daily sheets", $"{jobHref}?tab=daily-work&dailyView=sheets");
        }

        if (pendingMrns > 0 || pendingDispositions > 0)
        {
            return ("Clear material records", $"{jobHref}?tab=materials");
        }

        if (pendingIous > 0 || pendingClaims > 0)
        {
            return ("Review expenses", $"{jobHref}?tab=expenses");
        }

        if (status == ServiceJobStatus.WorkCompleted && !hasCompletedServiceTaken)
        {
            return ("Create service taken", "/service/handovers");
        }

        if ((status == ServiceJobStatus.WorkCompleted || status == ServiceJobStatus.ReadyForInvoice) && hasCompletedServiceTaken && !finalInvoiceNotRequired)
        {
            return ("Prepare invoice", $"{jobHref}?tab=billing");
        }

        if (status == ServiceJobStatus.Invoiced || finalInvoiceNotRequired)
        {
            return ("Close job", $"{jobHref}?tab=billing");
        }

        return ("Open job cockpit", jobHref);
    }
}

using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Application.Common;
using ISS.Domain.Common;
using ISS.Domain.Inventory;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/handovers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class ServiceHandoversController(
    IIssDbContext dbContext,
    ServiceManagementService serviceManagementService,
    IDocumentPdfService pdfService) : ControllerBase
{
    public sealed record ServiceHandoverDto(
        Guid Id,
        string Number,
        Guid ServiceJobId,
        DateTimeOffset HandoverDate,
        string ItemsReturned,
        int? PostServiceWarrantyMonths,
        string? CustomerAcknowledgement,
        string? Notes,
        ServiceHandoverStatus Status,
        Guid? SalesInvoiceId,
        string? SalesInvoiceNumber,
        DateTimeOffset? ConvertedToInvoiceAt);

    public sealed record CreateServiceHandoverRequest(
        Guid ServiceJobId,
        string ItemsReturned,
        int? PostServiceWarrantyMonths,
        string? CustomerAcknowledgement,
        string? Notes);
    public sealed record UpdateServiceHandoverRequest(
        string ItemsReturned,
        int? PostServiceWarrantyMonths,
        string? CustomerAcknowledgement,
        string? Notes);
    public sealed record ConvertToSalesInvoiceRequest(
        Guid? ServiceEstimateId,
        Guid? LaborItemId,
        Guid? ExpenseItemId,
        ServiceLaborBillingSource? LaborBillingSource,
        DateTimeOffset? DueDate,
        IReadOnlyList<ConvertToSalesInvoiceLineRequest>? ManualLines);
    public sealed record ConvertToSalesInvoiceLineRequest(
        Guid ItemId,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent,
        Guid? MaterialRequisitionLineId,
        string? Description = null);

    /// <summary>Material issued to the job, with how much of it has already been invoiced.</summary>
    public sealed record BillableIssuedMaterialDto(
        Guid MaterialRequisitionLineId,
        string MaterialRequisitionNumber,
        Guid ItemId,
        string ItemSku,
        string ItemName,
        decimal IssuedQuantity,
        decimal ReturnedQuantity,
        decimal NetQuantity,
        decimal AlreadyInvoicedQuantity,
        decimal RemainingQuantity,
        decimal UnitCost);
    /// <summary>Approved billable labour on the job that has not reached an invoice yet.</summary>
    public sealed record BillableLabourDto(
        Guid TimeEntryId,
        DateTimeOffset WorkDate,
        string TechnicianName,
        string WorkDescription,
        decimal HoursWorked,
        decimal CostRate,
        decimal LabourCost,
        decimal BillableHours,
        decimal BillingRate,
        decimal TaxPercent,
        decimal SuggestedTotal);

    /// <summary>Approved billable expense-claim lines that can still be recharged.</summary>
    public sealed record BillableExpenseDto(
        Guid ExpenseClaimLineId,
        string ExpenseClaimNumber,
        DateTimeOffset ExpenseDate,
        string Description,
        decimal Quantity,
        decimal UnitCost,
        decimal LineTotal);

    /// <summary>
    /// Everything the job can still charge the customer for, in one payload, so the billing screen
    /// does not have to stitch three sources together.
    /// </summary>
    public sealed record BillableChargesDto(
        Guid ServiceJobId,
        string JobNumber,
        ServiceCoverageScope EntitlementCoverage,
        string? EntitlementSummary,
        bool PartsCoveredByEntitlement,
        bool LabourCoveredByEntitlement,
        IReadOnlyList<BillableIssuedMaterialDto> Materials,
        IReadOnlyList<BillableLabourDto> Labour,
        IReadOnlyList<BillableExpenseDto> Expenses);

    public sealed record BuildServiceInvoiceRequest(
        DateTimeOffset? DueDate,
        decimal HeaderDiscountPercent,
        decimal HeaderDiscountAmount,
        IReadOnlyList<ConvertToSalesInvoiceLineRequest>? MaterialLines,
        ServiceManagementService.ServiceChargeBillingMode LabourMode,
        Guid? LabourItemId,
        IReadOnlyList<BuildServiceInvoiceLabourRequest>? LabourCharges,
        ServiceManagementService.ServiceChargeBillingMode ExpenseMode,
        Guid? ExpenseItemId,
        IReadOnlyList<BuildServiceInvoiceExpenseRequest>? ExpenseCharges,
        IReadOnlyList<ConvertToSalesInvoiceLineRequest>? OtherLines);

    public sealed record BuildServiceInvoiceLabourRequest(
        Guid TimeEntryId,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent);

    public sealed record BuildServiceInvoiceExpenseRequest(
        Guid ExpenseClaimLineId,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent);

    public sealed record ConvertToSalesInvoiceResponse(Guid SalesInvoiceId);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceHandoverDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.ServiceHandovers.AsNoTracking()
            .OrderByDescending(x => x.HandoverDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new ServiceHandoverDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.HandoverDate,
                x.ItemsReturned,
                x.PostServiceWarrantyMonths,
                x.CustomerAcknowledgement,
                x.Notes,
                x.Status,
                x.SalesInvoiceId,
                x.SalesInvoiceId == null
                    ? null
                    : dbContext.SalesInvoices
                        .Where(i => i.Id == x.SalesInvoiceId.Value)
                        .Select(i => i.Number)
                        .FirstOrDefault(),
                x.ConvertedToInvoiceAt))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceHandoverDto>> Create(CreateServiceHandoverRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateServiceHandoverAsync(
            request.ServiceJobId,
            request.ItemsReturned,
            request.PostServiceWarrantyMonths,
            request.CustomerAcknowledgement,
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceHandoverDto>> Update(Guid id, UpdateServiceHandoverRequest request, CancellationToken cancellationToken)
    {
        await serviceManagementService.UpdateServiceHandoverAsync(
            id,
            request.ItemsReturned,
            request.PostServiceWarrantyMonths,
            request.CustomerAcknowledgement,
            request.Notes,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceHandoverDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var handover = await dbContext.ServiceHandovers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ServiceHandoverDto(
                x.Id,
                x.Number,
                x.ServiceJobId,
                x.HandoverDate,
                x.ItemsReturned,
                x.PostServiceWarrantyMonths,
                x.CustomerAcknowledgement,
                x.Notes,
                x.Status,
                x.SalesInvoiceId,
                x.SalesInvoiceId == null
                    ? null
                    : dbContext.SalesInvoices
                        .Where(i => i.Id == x.SalesInvoiceId.Value)
                        .Select(i => i.Number)
                        .FirstOrDefault(),
                x.ConvertedToInvoiceAt))
            .FirstOrDefaultAsync(cancellationToken);

        return handover is null ? NotFound() : Ok(handover);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var doc = await pdfService.RenderAsync(PdfDocumentType.ServiceHandover, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CompleteServiceHandoverAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await serviceManagementService.CancelServiceHandoverAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// The job's issued materials with cost, so the invoice builder can offer them instead of the
    /// user going back to the MRN. Quantity already billed is derived from the material link on
    /// existing invoice lines, so the same issue is not charged twice across invoices.
    /// </summary>
    [HttpGet("{id:guid}/billable-materials")]
    public async Task<ActionResult<IReadOnlyList<BillableIssuedMaterialDto>>> BillableMaterials(Guid id, CancellationToken cancellationToken)
    {
        var handover = await dbContext.ServiceHandovers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (handover is null)
        {
            return NotFound();
        }

        return Ok(await LoadBillableMaterialsAsync(handover.ServiceJobId, cancellationToken));
    }

    private async Task<IReadOnlyList<BillableIssuedMaterialDto>> LoadBillableMaterialsAsync(
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        var issued = await (
            from movement in dbContext.InventoryMovements.AsNoTracking()
            join requisition in dbContext.MaterialRequisitions.AsNoTracking() on movement.ReferenceId equals requisition.Id
            join item in dbContext.Items.AsNoTracking() on movement.ItemId equals item.Id
            where movement.ReferenceType == ReferenceTypes.MaterialRequisition
                  && movement.Type == InventoryMovementType.Consumption
                  && requisition.ServiceJobId == serviceJobId
                  && requisition.Status == MaterialRequisitionStatus.Posted
            select new
            {
                LineId = movement.ReferenceLineId,
                RequisitionNumber = requisition.Number,
                ItemId = item.Id,
                item.Sku,
                item.Name,
                Quantity = -movement.Quantity,
                movement.UnitCost,
            }).ToListAsync(cancellationToken);

        var lineIds = issued.Where(x => x.LineId != null).Select(x => x.LineId!.Value).Distinct().ToList();

        var returned = lineIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await dbContext.ServiceJobMaterialDispositions.AsNoTracking()
                .Where(x => lineIds.Contains(x.MaterialRequisitionLineId)
                            && x.PostedAt != null && !x.IsVoided
                            && (x.Kind == ServiceJobMaterialDispositionKind.UnusedReturned
                                || x.Kind == ServiceJobMaterialDispositionKind.IncorrectReturned
                                || x.Kind == ServiceJobMaterialDispositionKind.RejectedSupplierReturn))
                .GroupBy(x => x.MaterialRequisitionLineId)
                .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.Quantity, cancellationToken);

        var invoiced = lineIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await dbContext.SalesInvoices.AsNoTracking()
                .Where(x => x.Status != SalesInvoiceStatus.Voided)
                .SelectMany(x => x.Lines)
                .Where(x => x.MaterialRequisitionLineId != null && lineIds.Contains(x.MaterialRequisitionLineId!.Value))
                .GroupBy(x => x.MaterialRequisitionLineId!.Value)
                .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.Quantity, cancellationToken);

        var rows = issued
            .Where(x => x.LineId != null)
            .GroupBy(x => x.LineId!.Value)
            .Select(g =>
            {
                var first = g.First();
                var issuedQty = g.Sum(x => x.Quantity);
                var returnedQty = returned.GetValueOrDefault(g.Key);
                var net = issuedQty - returnedQty;
                var billed = invoiced.GetValueOrDefault(g.Key);
                return new BillableIssuedMaterialDto(
                    g.Key,
                    first.RequisitionNumber,
                    first.ItemId,
                    first.Sku,
                    first.Name,
                    issuedQty,
                    returnedQty,
                    net,
                    billed,
                    Math.Max(0m, net - billed),
                    first.UnitCost);
            })
            .OrderBy(x => x.ItemSku)
            .ToList();

        return rows;
    }

    /// <summary>
    /// Materials, approved labour and recharged expenses the job can still bill, with what has
    /// already been charged deducted. This is the single source the billing screen reads.
    /// </summary>
    [HttpGet("{id:guid}/billable-charges")]
    public async Task<ActionResult<BillableChargesDto>> BillableCharges(Guid id, CancellationToken cancellationToken)
    {
        var handover = await dbContext.ServiceHandovers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (handover is null)
        {
            return NotFound();
        }

        var job = await dbContext.ServiceJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == handover.ServiceJobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var materials = await LoadBillableMaterialsAsync(handover.ServiceJobId, cancellationToken);

        var labour = await dbContext.WorkOrderTimeEntries.AsNoTracking()
            .Where(x => x.ServiceJobId == job.Id
                        && x.Status == WorkOrderTimeEntryStatus.Approved
                        && x.BillableToCustomer
                        && x.SalesInvoiceLineId == null)
            .OrderBy(x => x.WorkDate)
            .Select(x => new BillableLabourDto(
                x.Id,
                x.WorkDate,
                x.TechnicianName,
                x.WorkDescription,
                x.HoursWorked,
                x.CostRate,
                x.HoursWorked * x.CostRate,
                x.BillableHours,
                x.BillingRate,
                x.TaxPercent,
                x.BillableHours * x.BillingRate))
            .ToListAsync(cancellationToken);

        var expenses = await (
            from claim in dbContext.ServiceExpenseClaims.AsNoTracking()
            from line in claim.Lines
            where claim.ServiceJobId == job.Id
                  && (claim.Status == ServiceExpenseClaimStatus.Approved || claim.Status == ServiceExpenseClaimStatus.Settled)
                  && line.BillableToCustomer
                  && line.SalesInvoiceLineId == null
            orderby claim.ExpenseDate
            select new BillableExpenseDto(
                line.Id,
                claim.Number,
                claim.ExpenseDate,
                line.Description,
                line.Quantity,
                line.UnitCost,
                line.Quantity * line.UnitCost)).ToListAsync(cancellationToken);

        return Ok(new BillableChargesDto(
            job.Id,
            job.Number,
            job.EntitlementCoverage,
            job.EntitlementSummary,
            ServiceEntitlementRules.ApplyEstimateUnitPrice(job.EntitlementCoverage, ServiceEstimateLineKind.Part, 1m) == 0m,
            ServiceEntitlementRules.ApplyEstimateUnitPrice(job.EntitlementCoverage, ServiceEstimateLineKind.Labor, 1m) == 0m,
            materials,
            labour,
            expenses));
    }

    /// <summary>
    /// Creates the customer's invoice from the charges chosen on the billing screen. Replaces the
    /// old direct-line path, which could not see labour or expenses at all.
    /// </summary>
    [HttpPost("{id:guid}/build-invoice")]
    public async Task<ActionResult<ConvertToSalesInvoiceResponse>> BuildInvoice(
        Guid id,
        BuildServiceInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var input = new ServiceManagementService.ServiceInvoiceBuildInput(
            request.DueDate,
            request.HeaderDiscountPercent,
            request.HeaderDiscountAmount,
            (request.MaterialLines ?? [])
                .Select(line => new ServiceManagementService.ServiceInvoiceManualLineInput(
                    line.ItemId,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    line.MaterialRequisitionLineId,
                    line.Description))
                .ToList(),
            request.LabourMode,
            request.LabourItemId,
            (request.LabourCharges ?? [])
                .Select(charge => new ServiceManagementService.ServiceInvoiceLabourChargeInput(
                    charge.TimeEntryId,
                    charge.Quantity,
                    charge.UnitPrice,
                    charge.DiscountPercent,
                    charge.TaxPercent))
                .ToList(),
            request.ExpenseMode,
            request.ExpenseItemId,
            (request.ExpenseCharges ?? [])
                .Select(charge => new ServiceManagementService.ServiceInvoiceExpenseChargeInput(
                    charge.ExpenseClaimLineId,
                    charge.Quantity,
                    charge.UnitPrice,
                    charge.DiscountPercent,
                    charge.TaxPercent))
                .ToList(),
            (request.OtherLines ?? [])
                .Select(line => new ServiceManagementService.ServiceInvoiceManualLineInput(
                    line.ItemId,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    MaterialRequisitionLineId: null,
                    line.Description))
                .ToList());

        var salesInvoiceId = await serviceManagementService.BuildServiceInvoiceFromChargesAsync(id, input, cancellationToken);
        return Ok(new ConvertToSalesInvoiceResponse(salesInvoiceId));
    }

    [HttpPost("{id:guid}/convert-to-sales-invoice")]
    public async Task<ActionResult<ConvertToSalesInvoiceResponse>> ConvertToSalesInvoice(
        Guid id,
        ConvertToSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var salesInvoiceId = await serviceManagementService.ConvertServiceHandoverToSalesInvoiceAsync(
            id,
            request.ServiceEstimateId,
            request.LaborItemId,
            request.ExpenseItemId,
            request.LaborBillingSource ?? ServiceLaborBillingSource.Auto,
            request.DueDate,
            request.ManualLines?
                .Select(line => new ServiceManagementService.ServiceInvoiceManualLineInput(
                    line.ItemId,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    line.MaterialRequisitionLineId,
                    line.Description))
                .ToList(),
            cancellationToken);

        return Ok(new ConvertToSalesInvoiceResponse(salesInvoiceId));
    }
}

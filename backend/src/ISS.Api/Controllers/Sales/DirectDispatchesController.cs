using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Sales;

[ApiController]
[Route("api/sales/direct-dispatches")]
[Authorize]
public sealed class DirectDispatchesController(
    IIssDbContext dbContext,
    SalesService salesService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record DirectDispatchSummaryDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        string? Reason,
        int LineCount);

    public sealed record DirectDispatchLineDto(Guid Id, Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string> Serials);
    public sealed record DirectDispatchDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        Guid? CustomerId,
        Guid? ServiceJobId,
        DateTimeOffset DispatchedAt,
        DirectDispatchStatus Status,
        DateTimeOffset? WarrantyUntil,
        ServiceCoverageScope WarrantyCoverage,
        int? ServiceIntervalDays,
        DateTimeOffset? NextServiceDueAt,
        string? Reason,
        Guid? MaterialRequisitionId,
        IReadOnlyList<DirectDispatchLineDto> Lines);

    public sealed record CreateDirectDispatchRequest(Guid WarehouseId, Guid? CustomerId, Guid? ServiceJobId, string? Reason, DateTimeOffset? WarrantyUntil, ServiceCoverageScope? WarrantyCoverage, int? ServiceIntervalDays, DateTimeOffset? NextServiceDueAt);
    public sealed record AddDirectDispatchLineRequest(Guid ItemId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateDirectDispatchLineRequest(decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record MrnPlanDto(
        Guid MaterialRequisitionId,
        string RequisitionNumber,
        IReadOnlyList<MrnPlanLineDto> Lines);

    public sealed record MrnPlanLineDto(
        Guid MaterialRequisitionLineId,
        Guid ItemId,
        decimal RequestedQuantity,
        decimal PreviouslyDispatchedQuantity,
        decimal ReservedInOtherDraftsQuantity,
        decimal AvailableQuantity,
        Guid? DirectDispatchLineId,
        decimal CurrentQuantity,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record UpdateMrnPlanRequest(Guid MaterialRequisitionId, IReadOnlyList<UpdateMrnPlanLineRequest> Lines);
    public sealed record UpdateMrnPlanLineRequest(Guid MaterialRequisitionLineId, decimal Quantity, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DirectDispatchSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var rows = await dbContext.DirectDispatches.AsNoTracking()
            .OrderByDescending(x => x.DispatchedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new DirectDispatchSummaryDto(
                x.Id,
                x.Number,
                x.WarehouseId,
                x.CustomerId,
                x.ServiceJobId,
                x.DispatchedAt,
                x.Status,
                x.WarrantyUntil,
                x.WarrantyCoverage,
                x.ServiceIntervalDays,
                x.NextServiceDueAt,
                x.Reason,
                x.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<DirectDispatchDto>> Create(CreateDirectDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await salesService.CreateDirectDispatchAsync(
            request.WarehouseId,
            request.CustomerId,
            request.ServiceJobId,
            request.Reason,
            request.WarrantyUntil,
            request.WarrantyUntil is null ? ServiceCoverageScope.None : request.WarrantyCoverage ?? ServiceCoverageScope.LaborAndParts,
            request.ServiceIntervalDays,
            request.NextServiceDueAt,
            cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DirectDispatchDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (dispatch is null)
        {
            return NotFound();
        }

        return Ok(new DirectDispatchDto(
            dispatch.Id,
            dispatch.Number,
            dispatch.WarehouseId,
            dispatch.CustomerId,
            dispatch.ServiceJobId,
            dispatch.DispatchedAt,
            dispatch.Status,
            dispatch.WarrantyUntil,
            dispatch.WarrantyCoverage,
            dispatch.ServiceIntervalDays,
            dispatch.NextServiceDueAt,
            dispatch.Reason,
            dispatch.MaterialRequisitionId,
            dispatch.Lines.Select(l => new DirectDispatchLineDto(l.Id, l.ItemId, l.Quantity, l.BatchNumber, l.Serials.Select(s => s.SerialNumber).ToList())).ToList()));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.DirectDispatch, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddDirectDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.AddDirectDispatchLineAsync(id, request.ItemId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// The requested lines of a material requisition alongside what this draft already
    /// dispatches, so the user can enter full or partial quantities against each request.
    /// Mirrors the goods receipt plan built against a purchase order.
    /// </summary>
    [HttpGet("{id:guid}/mrn-plan")]
    public async Task<ActionResult<MrnPlanDto>> MrnPlan(
        Guid id,
        [FromQuery] Guid? materialRequisitionId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchView, cancellationToken))
        {
            return Forbid();
        }

        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (dispatch is null)
        {
            return NotFound();
        }

        var requisitionId = materialRequisitionId ?? dispatch.MaterialRequisitionId;
        if (requisitionId is null)
        {
            return NoContent();
        }

        var plan = await BuildMrnPlanAsync(dispatch, requisitionId.Value, cancellationToken);
        return plan is null ? BadRequest("Selected material requisition does not exist.") : Ok(plan);
    }

    /// <summary>
    /// Saves the entered quantities. A line set to zero or blank is removed, so the grid is the
    /// single place the draft's contents are managed.
    /// </summary>
    [HttpPut("{id:guid}/mrn-plan")]
    public async Task<ActionResult<MrnPlanDto>> UpdateMrnPlan(Guid id, UpdateMrnPlanRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        var dispatch = await dbContext.DirectDispatches
            .Include(x => x.Lines).ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (dispatch is null)
        {
            return NotFound();
        }

        if (dispatch.Status != DirectDispatchStatus.Draft)
        {
            return BadRequest("Only a draft dispatch can be edited.");
        }

        var requisition = await dbContext.MaterialRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.MaterialRequisitionId, cancellationToken);
        if (requisition is null)
        {
            return BadRequest("Selected material requisition does not exist.");
        }

        if (dispatch.ServiceJobId is { } jobId && requisition.ServiceJobId != jobId)
        {
            return BadRequest("That material requisition belongs to a different job order.");
        }

        var requestedById = requisition.Lines.ToDictionary(x => x.Id);
        var existingByRequisitionLine = dispatch.Lines
            .Where(x => x.MaterialRequisitionLineId != null)
            .ToDictionary(x => x.MaterialRequisitionLineId!.Value);

        foreach (var line in request.Lines)
        {
            if (!requestedById.TryGetValue(line.MaterialRequisitionLineId, out var requested))
            {
                return BadRequest("A submitted line does not belong to that material requisition.");
            }

            var quantity = line.Quantity;
            existingByRequisitionLine.TryGetValue(line.MaterialRequisitionLineId, out var existing);

            if (quantity <= 0)
            {
                if (existing is not null)
                {
                    dispatch.RemoveLine(existing.Id);
                }

                continue;
            }

            if (existing is null)
            {
                var created = dispatch.AddLine(requested.ItemId, quantity, line.BatchNumber, requested.Id);
                created.ReplaceSerials(line.Serials);
                dbContext.DbContext.Add(created);
            }
            else
            {
                dispatch.UpdateLine(existing.Id, quantity, line.BatchNumber, line.Serials);
            }
        }

        dispatch.LinkMaterialRequisition(requisition.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var refreshed = await dbContext.DirectDispatches.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Serials)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return Ok(await BuildMrnPlanAsync(refreshed, requisition.Id, cancellationToken));
    }

    private async Task<MrnPlanDto?> BuildMrnPlanAsync(DirectDispatch dispatch, Guid requisitionId, CancellationToken cancellationToken)
    {
        var requisition = await dbContext.MaterialRequisitions.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == requisitionId, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        // what other dispatches have already taken against each requested line
        var otherLines = await dbContext.DirectDispatches.AsNoTracking()
            .Where(x => x.MaterialRequisitionId == requisitionId && x.Id != dispatch.Id && x.Status != DirectDispatchStatus.Voided)
            .SelectMany(x => x.Lines.Select(l => new { x.Status, l.MaterialRequisitionLineId, l.Quantity }))
            .Where(x => x.MaterialRequisitionLineId != null)
            .ToListAsync(cancellationToken);

        var posted = otherLines.Where(x => x.Status == DirectDispatchStatus.Posted)
            .GroupBy(x => x.MaterialRequisitionLineId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var reserved = otherLines.Where(x => x.Status == DirectDispatchStatus.Draft)
            .GroupBy(x => x.MaterialRequisitionLineId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var currentByRequisitionLine = dispatch.Lines
            .Where(x => x.MaterialRequisitionLineId != null)
            .ToDictionary(x => x.MaterialRequisitionLineId!.Value);

        var lines = requisition.Lines.Select(requested =>
        {
            currentByRequisitionLine.TryGetValue(requested.Id, out var current);
            var previously = posted.GetValueOrDefault(requested.Id);
            var reservedQty = reserved.GetValueOrDefault(requested.Id);

            return new MrnPlanLineDto(
                requested.Id,
                requested.ItemId,
                requested.Quantity,
                previously,
                reservedQty,
                Math.Max(0m, requested.Quantity - previously - reservedQty),
                current?.Id,
                current?.Quantity ?? 0m,
                current?.BatchNumber ?? requested.BatchNumber,
                current?.Serials.Select(s => s.SerialNumber).ToList()
                    ?? requested.Serials.Select(s => s.SerialNumber).ToList());
        }).ToList();

        return new MrnPlanDto(requisition.Id, requisition.Number, lines);
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateDirectDispatchLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.UpdateDirectDispatchLineAsync(id, lineId, request.Quantity, request.BatchNumber, request.Serials, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchEdit, cancellationToken))
        {
            return Forbid();
        }

        await salesService.RemoveDirectDispatchLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.SalesDirectDispatchPost, cancellationToken))
        {
            return Forbid();
        }

        await salesService.PostDirectDispatchAsync(id, cancellationToken);
        await NotifyDirectDispatchCreatorAsync(id, "Direct dispatch posted", "Your direct dispatch has been posted.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyDirectDispatchCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var dispatch = await dbContext.DirectDispatches.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (dispatch is null || dispatch.CreatedBy is null || dispatch.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            dispatch.CreatedBy.Value,
            title,
            $"{dispatch.Number}: {message}",
            $"/sales/direct-dispatches/{dispatch.Id}",
            ReferenceTypes.DirectDispatch,
            dispatch.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

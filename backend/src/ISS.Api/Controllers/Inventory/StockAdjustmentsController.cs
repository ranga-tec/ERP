using System.Security.Claims;
using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Inventory;

[ApiController]
[Route("api/inventory/stock-adjustments")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class StockAdjustmentsController(
    IIssDbContext dbContext,
    InventoryOperationsService inventoryOperationsService,
    IDocumentPdfService pdfService,
    AccessControlService accessControl,
    NotificationService notificationService) : ControllerBase
{
    public sealed record StockAdjustmentSummaryDto(Guid Id, string Number, Guid WarehouseId, DateTimeOffset AdjustedAt, StockAdjustmentStatus Status);

    public sealed record StockAdjustmentDto(
        Guid Id,
        string Number,
        Guid WarehouseId,
        DateTimeOffset AdjustedAt,
        StockAdjustmentStatus Status,
        string? Reason,
        IReadOnlyList<StockAdjustmentLineDto> Lines);

    public sealed record StockAdjustmentLineDto(
        Guid Id,
        Guid ItemId,
        decimal? CountedQuantity,
        decimal? SystemQuantity,
        decimal QuantityDelta,
        decimal UnitCost,
        string? BatchNumber,
        IReadOnlyList<string> Serials);

    public sealed record CreateStockAdjustmentRequest(Guid WarehouseId, string? Reason);
    public sealed record AddStockAdjustmentLineRequest(Guid ItemId, decimal? CountedQuantity, decimal? QuantityDelta, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);
    public sealed record UpdateStockAdjustmentLineRequest(decimal? CountedQuantity, decimal? QuantityDelta, decimal UnitCost, string? BatchNumber, IReadOnlyList<string>? Serials);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentSummaryDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentView, cancellationToken))
        {
            return Forbid();
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var adjustments = await dbContext.StockAdjustments.AsNoTracking()
            .OrderByDescending(x => x.AdjustedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new StockAdjustmentSummaryDto(x.Id, x.Number, x.WarehouseId, x.AdjustedAt, x.Status))
            .ToListAsync(cancellationToken);

        return Ok(adjustments);
    }

    [HttpPost]
    public async Task<ActionResult<StockAdjustmentDto>> Create(CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentCreate, cancellationToken))
        {
            return Forbid();
        }

        var id = await inventoryOperationsService.CreateStockAdjustmentAsync(request.WarehouseId, request.Reason, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockAdjustmentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentView, cancellationToken))
        {
            return Forbid();
        }

        var adj = await dbContext.StockAdjustments.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(l => l.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (adj is null)
        {
            return NotFound();
        }

        var lines = new List<StockAdjustmentLineDto>(adj.Lines.Count);
        foreach (var line in adj.Lines)
        {
            decimal? systemQuantity = line.SystemQuantity;
            var countedQuantity = line.CountedQuantity;
            var quantityDelta = line.QuantityDelta;

            if (adj.Status == StockAdjustmentStatus.Draft)
            {
                systemQuantity = await inventoryOperationsService.GetStockAdjustmentPreviewQuantityAsync(
                    adj.WarehouseId,
                    line.ItemId,
                    line.BatchNumber,
                    cancellationToken);

                if (countedQuantity is null)
                {
                    countedQuantity = systemQuantity + line.QuantityDelta;
                }
                else
                {
                    quantityDelta = countedQuantity.Value - systemQuantity.Value;
                }
            }
            else if (countedQuantity is null && systemQuantity is not null)
            {
                countedQuantity = systemQuantity + quantityDelta;
            }

            lines.Add(new StockAdjustmentLineDto(
                line.Id,
                line.ItemId,
                countedQuantity,
                systemQuantity,
                quantityDelta,
                line.UnitCost,
                line.BatchNumber,
                line.Serials.Select(s => s.SerialNumber).ToList()));
        }

        return Ok(new StockAdjustmentDto(
            adj.Id,
            adj.Number,
            adj.WarehouseId,
            adj.AdjustedAt,
            adj.Status,
            adj.Reason,
            lines));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentView, cancellationToken))
        {
            return Forbid();
        }

        var doc = await pdfService.RenderAsync(PdfDocumentType.StockAdjustment, id, cancellationToken);
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult> AddLine(Guid id, AddStockAdjustmentLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentEdit, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.AddStockAdjustmentLineAsync(
            id,
            request.ItemId,
            request.CountedQuantity,
            request.QuantityDelta,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> UpdateLine(Guid id, Guid lineId, UpdateStockAdjustmentLineRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentEdit, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.UpdateStockAdjustmentLineAsync(
            id,
            lineId,
            request.CountedQuantity,
            request.QuantityDelta,
            request.UnitCost,
            request.BatchNumber,
            request.Serials,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentEdit, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.RemoveStockAdjustmentLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentPost, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.PostStockAdjustmentAsync(id, cancellationToken);
        await NotifyStockAdjustmentCreatorAsync(id, "Stock adjustment posted", "Your stock adjustment has been posted.", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(AppPermissions.InventoryStockAdjustmentVoid, cancellationToken))
        {
            return Forbid();
        }

        await inventoryOperationsService.VoidStockAdjustmentAsync(id, cancellationToken);
        await NotifyStockAdjustmentCreatorAsync(id, "Stock adjustment voided", "Your stock adjustment has been voided.", cancellationToken);
        return NoContent();
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
               && await accessControl.HasPermissionAsync(userId, permissionKey, cancellationToken);
    }

    private async Task NotifyStockAdjustmentCreatorAsync(Guid id, string title, string message, CancellationToken cancellationToken)
    {
        var adjustment = await dbContext.StockAdjustments.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Number, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (adjustment is null || adjustment.CreatedBy is null || adjustment.CreatedBy == Guid.Empty)
        {
            return;
        }

        notificationService.EnqueueInApp(
            adjustment.CreatedBy.Value,
            title,
            $"{adjustment.Number}: {message}",
            $"/inventory/stock-adjustments/{adjustment.Id}",
            ReferenceTypes.StockAdjustment,
            adjustment.Id);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

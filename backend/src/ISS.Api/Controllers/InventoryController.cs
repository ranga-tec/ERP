using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Reporting}")]
public sealed class InventoryController(IIssDbContext dbContext, InventoryService inventoryService, ProcurementService procurementService) : ControllerBase
{
    public sealed record OnHandDto(Guid WarehouseId, Guid ItemId, string? BatchNumber, decimal OnHand);

    [HttpGet("onhand")]
    public async Task<ActionResult<OnHandDto>> GetOnHand([FromQuery] Guid warehouseId, [FromQuery] Guid itemId, [FromQuery] string? batchNumber, CancellationToken cancellationToken)
    {
        var onHand = await inventoryService.GetOnHandAsync(warehouseId, itemId, batchNumber, cancellationToken);
        return Ok(new OnHandDto(warehouseId, itemId, batchNumber?.Trim(), onHand));
    }

    public sealed record ReorderAlertDto(Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity, decimal OnHand);
    public sealed record CreateReorderPurchaseRequisitionRequest(Guid WarehouseId, string? Notes, bool Submit = false);
    public sealed record CreateReorderPurchaseRequisitionResponseDto(Guid PurchaseRequisitionId, string PurchaseRequisitionNumber, int LineCount, decimal TotalSuggestedQuantity);

    [HttpGet("reorder-alerts")]
    public async Task<ActionResult<IReadOnlyList<ReorderAlertDto>>> ReorderAlerts([FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var settingsQuery = dbContext.ReorderSettings.AsNoTracking();
        if (warehouseId is not null && warehouseId != Guid.Empty)
        {
            settingsQuery = settingsQuery.Where(x => x.WarehouseId == warehouseId);
        }

        var settings = await settingsQuery.ToListAsync(cancellationToken);
        var alerts = new List<ReorderAlertDto>();

        foreach (var s in settings)
        {
            var onHand = await inventoryService.GetOnHandAsync(s.WarehouseId, s.ItemId, batchNumber: null, cancellationToken);
            if (onHand <= s.ReorderPoint)
            {
                alerts.Add(new ReorderAlertDto(s.WarehouseId, s.ItemId, s.ReorderPoint, s.ReorderQuantity, onHand));
            }
        }

        return Ok(alerts.OrderBy(a => a.WarehouseId).ThenBy(a => a.ItemId).ToList());
    }

    [HttpPost("reorder-alerts/create-purchase-requisition")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Procurement}")]
    public async Task<ActionResult<CreateReorderPurchaseRequisitionResponseDto>> CreatePurchaseRequisitionFromReorderAlerts(
        CreateReorderPurchaseRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.WarehouseId == Guid.Empty)
        {
            return BadRequest("warehouseId is required.");
        }

        var settings = await dbContext.ReorderSettings.AsNoTracking()
            .Where(x => x.WarehouseId == request.WarehouseId)
            .OrderBy(x => x.ItemId)
            .ToListAsync(cancellationToken);

        if (settings.Count == 0)
        {
            return BadRequest("No reorder settings found for the selected warehouse.");
        }

        var alerts = new List<(Guid ItemId, decimal SuggestedQuantity, decimal ReorderPoint, decimal ReorderQuantity, decimal OnHand)>();
        foreach (var setting in settings)
        {
            var onHand = await inventoryService.GetOnHandAsync(setting.WarehouseId, setting.ItemId, batchNumber: null, cancellationToken);
            if (onHand > setting.ReorderPoint)
            {
                continue;
            }

            var shortage = setting.ReorderPoint - onHand;
            var suggestedQty = setting.ReorderQuantity > 0m
                ? setting.ReorderQuantity
                : (shortage > 0m ? shortage : 1m);

            alerts.Add((setting.ItemId, suggestedQty, setting.ReorderPoint, setting.ReorderQuantity, onHand));
        }

        if (alerts.Count == 0)
        {
            return BadRequest("No items are currently at or below reorder point for the selected warehouse.");
        }

        var warehouse = await dbContext.Warehouses.AsNoTracking()
            .Where(x => x.Id == request.WarehouseId)
            .Select(x => new { x.Code })
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null)
        {
            return NotFound("Warehouse not found.");
        }

        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? $"Auto-generated from reorder alerts for warehouse {warehouse.Code} on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC."
            : request.Notes;

        var purchaseRequisitionId = await procurementService.CreatePurchaseRequisitionAsync(notes, cancellationToken);
        foreach (var alert in alerts)
        {
            var lineNotes = $"Reorder alert: on hand {alert.OnHand}, reorder point {alert.ReorderPoint}, configured reorder qty {alert.ReorderQuantity}.";
            await procurementService.AddPurchaseRequisitionLineAsync(
                purchaseRequisitionId,
                alert.ItemId,
                alert.SuggestedQuantity,
                lineNotes,
                cancellationToken);
        }

        if (request.Submit)
        {
            await procurementService.SubmitPurchaseRequisitionAsync(purchaseRequisitionId, cancellationToken);
        }

        var pr = await dbContext.PurchaseRequisitions.AsNoTracking()
            .Where(x => x.Id == purchaseRequisitionId)
            .Select(x => new { x.Id, x.Number, LineCount = x.Lines.Count })
            .FirstAsync(cancellationToken);

        return Ok(new CreateReorderPurchaseRequisitionResponseDto(
            pr.Id,
            pr.Number,
            pr.LineCount,
            alerts.Sum(x => x.SuggestedQuantity)));
    }
}

using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Reporting}")]
public sealed class InventoryController(IIssDbContext dbContext, InventoryService inventoryService) : ControllerBase
{
    public sealed record OnHandDto(Guid WarehouseId, Guid ItemId, string? BatchNumber, decimal OnHand);

    [HttpGet("onhand")]
    public async Task<ActionResult<OnHandDto>> GetOnHand([FromQuery] Guid warehouseId, [FromQuery] Guid itemId, [FromQuery] string? batchNumber, CancellationToken cancellationToken)
    {
        var onHand = await inventoryService.GetOnHandAsync(warehouseId, itemId, batchNumber, cancellationToken);
        return Ok(new OnHandDto(warehouseId, itemId, batchNumber?.Trim(), onHand));
    }

    public sealed record ReorderAlertDto(Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity, decimal OnHand);

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
}


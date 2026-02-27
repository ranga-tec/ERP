using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/reorder-settings")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class ReorderSettingsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ReorderSettingDto(Guid Id, Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity);
    public sealed record UpsertReorderSettingRequest(Guid WarehouseId, Guid ItemId, decimal ReorderPoint, decimal ReorderQuantity);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReorderSettingDto>>> List(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ReorderSettings.AsNoTracking()
            .OrderBy(x => x.WarehouseId)
            .ThenBy(x => x.ItemId)
            .Select(x => new ReorderSettingDto(x.Id, x.WarehouseId, x.ItemId, x.ReorderPoint, x.ReorderQuantity))
            .ToListAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPost]
    public async Task<ActionResult<ReorderSettingDto>> Upsert(UpsertReorderSettingRequest request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ReorderSettings.FirstOrDefaultAsync(
            x => x.WarehouseId == request.WarehouseId && x.ItemId == request.ItemId,
            cancellationToken);

        if (existing is null)
        {
            var setting = new ReorderSetting(request.WarehouseId, request.ItemId, request.ReorderPoint, request.ReorderQuantity);
            await dbContext.ReorderSettings.AddAsync(setting, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new ReorderSettingDto(setting.Id, setting.WarehouseId, setting.ItemId, setting.ReorderPoint, setting.ReorderQuantity));
        }

        existing.Update(request.ReorderPoint, request.ReorderQuantity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new ReorderSettingDto(existing.Id, existing.WarehouseId, existing.ItemId, existing.ReorderPoint, existing.ReorderQuantity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var setting = await dbContext.ReorderSettings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        dbContext.ReorderSettings.Remove(setting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

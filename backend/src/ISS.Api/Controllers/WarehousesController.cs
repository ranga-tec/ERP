using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Procurement},{Roles.Sales},{Roles.Service},{Roles.Finance},{Roles.Reporting}")]
public sealed class WarehousesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Address, bool IsActive);
    public sealed record WarehouseBinDto(Guid Id, Guid WarehouseId, string Code, string Name, string? Zone, string? Rack, string? Shelf, bool IsActive);
    public sealed record CreateWarehouseRequest(string Code, string Name, string? Address);
    public sealed record UpdateWarehouseRequest(string Code, string Name, string? Address, bool IsActive);
    public sealed record CreateWarehouseBinRequest(string Code, string Name, string? Zone, string? Rack, string? Shelf);
    public sealed record UpdateWarehouseBinRequest(string Code, string Name, string? Zone, string? Rack, string? Shelf, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> List(CancellationToken cancellationToken)
    {
        var warehouses = await dbContext.Warehouses.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(warehouses);
    }

    [HttpGet("bins")]
    public async Task<ActionResult<IReadOnlyList<WarehouseBinDto>>> ListBins(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.WarehouseBins.AsNoTracking();
        if (warehouseId is not null && warehouseId != Guid.Empty)
        {
            query = query.Where(x => x.WarehouseId == warehouseId);
        }

        var bins = await query
            .OrderBy(x => x.WarehouseId)
            .ThenBy(x => x.Code)
            .Select(x => new WarehouseBinDto(x.Id, x.WarehouseId, x.Code, x.Name, x.Zone, x.Rack, x.Shelf, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(bins);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await dbContext.Warehouses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.Address, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    [HttpGet("{id:guid}/bins")]
    public async Task<ActionResult<IReadOnlyList<WarehouseBinDto>>> ListBinsForWarehouse(Guid id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Warehouses.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var bins = await dbContext.WarehouseBins.AsNoTracking()
            .Where(x => x.WarehouseId == id)
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseBinDto(x.Id, x.WarehouseId, x.Code, x.Name, x.Zone, x.Rack, x.Shelf, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(bins);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<ActionResult<WarehouseDto>> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(request.Code, request.Name, request.Address);
        await dbContext.Warehouses.AddAsync(warehouse, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, new WarehouseDto(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.IsActive));
    }

    [HttpPost("{id:guid}/bins")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<ActionResult<WarehouseBinDto>> CreateBin(Guid id, CreateWarehouseBinRequest request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Warehouses.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var bin = new WarehouseBin(id, request.Code, request.Name, request.Zone, request.Rack, request.Shelf);
        await dbContext.WarehouseBins.AddAsync(bin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(ListBinsForWarehouse),
            new { id },
            new WarehouseBinDto(bin.Id, bin.WarehouseId, bin.Code, bin.Name, bin.Zone, bin.Rack, bin.Shelf, bin.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (warehouse is null)
        {
            return NotFound();
        }

        warehouse.Update(request.Code, request.Name, request.Address, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new WarehouseDto(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.IsActive));
    }

    [HttpPut("{warehouseId:guid}/bins/{binId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<ActionResult<WarehouseBinDto>> UpdateBin(Guid warehouseId, Guid binId, UpdateWarehouseBinRequest request, CancellationToken cancellationToken)
    {
        var bin = await dbContext.WarehouseBins.FirstOrDefaultAsync(x => x.Id == binId && x.WarehouseId == warehouseId, cancellationToken);
        if (bin is null)
        {
            return NotFound();
        }

        bin.Update(request.Code, request.Name, request.Zone, request.Rack, request.Shelf, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new WarehouseBinDto(bin.Id, bin.WarehouseId, bin.Code, bin.Name, bin.Zone, bin.Rack, bin.Shelf, bin.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (warehouse is null)
        {
            return NotFound();
        }

        dbContext.Warehouses.Remove(warehouse);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Warehouse is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }

    [HttpDelete("{warehouseId:guid}/bins/{binId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
    public async Task<IActionResult> DeleteBin(Guid warehouseId, Guid binId, CancellationToken cancellationToken)
    {
        var bin = await dbContext.WarehouseBins.FirstOrDefaultAsync(x => x.Id == binId && x.WarehouseId == warehouseId, cancellationToken);
        if (bin is null)
        {
            return NotFound();
        }

        dbContext.WarehouseBins.Remove(bin);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Bin is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

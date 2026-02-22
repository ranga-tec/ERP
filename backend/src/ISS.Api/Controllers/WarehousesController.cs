using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class WarehousesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Address, bool IsActive);
    public sealed record CreateWarehouseRequest(string Code, string Name, string? Address);
    public sealed record UpdateWarehouseRequest(string Code, string Name, string? Address, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> List(CancellationToken cancellationToken)
    {
        var warehouses = await dbContext.Warehouses.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(warehouses);
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

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(request.Code, request.Name, request.Address);
        await dbContext.Warehouses.AddAsync(warehouse, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, new WarehouseDto(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.IsActive));
    }

    [HttpPut("{id:guid}")]
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
}


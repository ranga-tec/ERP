using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/equipment-units")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service},{Roles.Sales}")]
public sealed class EquipmentUnitsController(IIssDbContext dbContext, ServiceManagementService serviceManagementService) : ControllerBase
{
    public sealed record EquipmentUnitDto(Guid Id, Guid ItemId, string SerialNumber, Guid CustomerId, DateTimeOffset? PurchasedAt, DateTimeOffset? WarrantyUntil);
    public sealed record CreateEquipmentUnitRequest(Guid ItemId, string SerialNumber, Guid CustomerId, DateTimeOffset? PurchasedAt, DateTimeOffset? WarrantyUntil);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentUnitDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var units = await dbContext.EquipmentUnits.AsNoTracking()
            .OrderBy(x => x.SerialNumber)
            .Skip(skip)
            .Take(take)
            .Select(x => new EquipmentUnitDto(x.Id, x.ItemId, x.SerialNumber, x.CustomerId, x.PurchasedAt, x.WarrantyUntil))
            .ToListAsync(cancellationToken);

        return Ok(units);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentUnitDto>> Create(CreateEquipmentUnitRequest request, CancellationToken cancellationToken)
    {
        var id = await serviceManagementService.CreateEquipmentUnitAsync(request.ItemId, request.SerialNumber, request.CustomerId, request.PurchasedAt, request.WarrantyUntil, cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentUnitDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var unit = await dbContext.EquipmentUnits.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EquipmentUnitDto(x.Id, x.ItemId, x.SerialNumber, x.CustomerId, x.PurchasedAt, x.WarrantyUntil))
            .FirstOrDefaultAsync(cancellationToken);

        return unit is null ? NotFound() : Ok(unit);
    }
}

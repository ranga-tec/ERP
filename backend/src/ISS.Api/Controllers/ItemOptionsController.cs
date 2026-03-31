using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/items/options")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory},{Roles.Procurement},{Roles.Sales},{Roles.Service},{Roles.Finance},{Roles.Reporting}")]
public sealed class ItemOptionsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ItemOptionDto(
        Guid Id,
        string Sku,
        string Name,
        ItemType Type,
        TrackingType TrackingType,
        decimal DefaultUnitCost,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemOptionDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.Items.AsNoTracking()
            .OrderBy(x => x.Sku)
            .Select(x => new ItemOptionDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Type,
                x.TrackingType,
                x.DefaultUnitCost,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}

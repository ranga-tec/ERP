using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/uoms")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class UnitOfMeasuresController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record UnitOfMeasureDto(Guid Id, string Code, string Name, bool IsActive);
    public sealed record CreateUnitOfMeasureRequest(string Code, string Name);
    public sealed record UpdateUnitOfMeasureRequest(string Code, string Name, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitOfMeasureDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.UnitOfMeasures.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new UnitOfMeasureDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.UnitOfMeasures.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UnitOfMeasureDto(x.Id, x.Code, x.Name, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UnitOfMeasureDto>> Create(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        var item = new UnitOfMeasure(request.Code, request.Name);
        await dbContext.UnitOfMeasures.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, new UnitOfMeasureDto(item.Id, item.Code, item.Name, item.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> Update(Guid id, UpdateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.UnitOfMeasures.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.Code, request.Name, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new UnitOfMeasureDto(item.Id, item.Code, item.Name, item.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.UnitOfMeasures.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.UnitOfMeasures.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("UoM is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

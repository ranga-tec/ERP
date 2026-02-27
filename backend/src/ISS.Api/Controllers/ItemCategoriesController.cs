using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/item-categories")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class ItemCategoriesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ItemCategoryDto(Guid Id, string Code, string Name, bool IsActive);
    public sealed record CreateItemCategoryRequest(string Code, string Name);
    public sealed record UpdateItemCategoryRequest(string Code, string Name, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemCategoryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.ItemCategories.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ItemCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemCategoryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemCategoryDto>> Create(CreateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var item = new ItemCategory(request.Code, request.Name);
        await dbContext.ItemCategories.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ItemCategoryDto(item.Id, item.Code, item.Name, item.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemCategoryDto>> Update(Guid id, UpdateItemCategoryRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.Code, request.Name, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new ItemCategoryDto(item.Id, item.Code, item.Name, item.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.ItemCategories.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Item category is in use and cannot be deleted. Remove dependent subcategories/items or mark inactive.");
        }

        return NoContent();
    }
}

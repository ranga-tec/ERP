using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/item-subcategories")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class ItemSubcategoriesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ItemSubcategoryDto(
        Guid Id,
        Guid CategoryId,
        string? CategoryCode,
        string? CategoryName,
        string Code,
        string Name,
        bool IsActive);

    public sealed record CreateItemSubcategoryRequest(Guid CategoryId, string Code, string Name);
    public sealed record UpdateItemSubcategoryRequest(Guid CategoryId, string Code, string Name, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemSubcategoryDto>>> List([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = dbContext.ItemSubcategories.AsNoTracking();
        if (categoryId is not null)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var items = await query
            .OrderBy(x => x.CategoryId)
            .ThenBy(x => x.Code)
            .Select(x => new ItemSubcategoryDto(
                x.Id,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.Code,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemSubcategoryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ItemSubcategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemSubcategoryDto(
                x.Id,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.Code,
                x.Name,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemSubcategoryDto>> Create(CreateItemSubcategoryRequest request, CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.ItemCategories.AsNoTracking()
            .AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return BadRequest("Selected category does not exist.");
        }

        var item = new ItemSubcategory(request.CategoryId, request.Code, request.Name);
        await dbContext.ItemSubcategories.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.ItemSubcategories.AsNoTracking()
            .Where(x => x.Id == item.Id)
            .Select(x => new ItemSubcategoryDto(
                x.Id,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.Code,
                x.Name,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemSubcategoryDto>> Update(Guid id, UpdateItemSubcategoryRequest request, CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.ItemCategories.AsNoTracking()
            .AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return BadRequest("Selected category does not exist.");
        }

        var item = await dbContext.ItemSubcategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.CategoryId, request.Code, request.Name, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.ItemSubcategories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemSubcategoryDto(
                x.Id,
                x.CategoryId,
                x.Category != null ? x.Category.Code : null,
                x.Category != null ? x.Category.Name : null,
                x.Code,
                x.Name,
                x.IsActive))
            .FirstAsync(cancellationToken);

        return Ok(updated);
    }
}

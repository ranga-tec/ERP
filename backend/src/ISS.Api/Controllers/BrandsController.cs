using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Inventory}")]
public sealed class BrandsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record BrandDto(Guid Id, string Code, string Name, bool IsActive);
    public sealed record CreateBrandRequest(string Code, string Name);
    public sealed record UpdateBrandRequest(string Code, string Name, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> List(CancellationToken cancellationToken)
    {
        var brands = await dbContext.Brands.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new BrandDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(brands);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BrandDto(x.Id, x.Code, x.Name, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return brand is null ? NotFound() : Ok(brand);
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = new Brand(request.Code, request.Name);
        await dbContext.Brands.AddAsync(brand, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = brand.Id }, new BrandDto(brand.Id, brand.Code, brand.Name, brand.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Update(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (brand is null)
        {
            return NotFound();
        }

        brand.Update(request.Code, request.Name, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new BrandDto(brand.Id, brand.Code, brand.Name, brand.IsActive));
    }
}


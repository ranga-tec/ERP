using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Finance}")]
public sealed class SuppliersController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record SupplierDto(Guid Id, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);
    public sealed record CreateSupplierRequest(string Code, string Name, string? Phone, string? Email, string? Address);
    public sealed record UpdateSupplierRequest(string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List(CancellationToken cancellationToken)
    {
        var suppliers = await dbContext.Suppliers.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier(request.Code, request.Name, request.Phone, request.Email, request.Address);
        await dbContext.Suppliers.AddAsync(supplier, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.Update(request.Code, request.Name, request.Phone, request.Email, request.Address, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        dbContext.Suppliers.Remove(supplier);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Supplier is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

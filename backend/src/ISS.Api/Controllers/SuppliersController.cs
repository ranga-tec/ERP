using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Finance}")]
public sealed class SuppliersController(IIssDbContext dbContext, ICurrentUser currentUser) : ControllerBase
{
    public sealed record SupplierDto(Guid Id, Guid CompanyId, string? CompanyCode, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);
    public sealed record CreateSupplierRequest(Guid? CompanyId, string Code, string Name, string? Phone, string? Email, string? Address);
    public sealed record UpdateSupplierRequest(Guid? CompanyId, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId);
        var query = dbContext.Suppliers.AsNoTracking().Where(x => x.CompanyId == resolvedCompanyId);

        var suppliers = await query
            .OrderBy(x => x.Code)
            .Select(x => new SupplierDto(x.Id, x.CompanyId, x.Company != null ? x.Company.Code : null, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.AsNoTracking()
            .Where(x => x.Id == id && x.CompanyId == ResolveCompanyId(null))
            .Select(x => new SupplierDto(x.Id, x.CompanyId, x.Company != null ? x.Company.Code : null, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId(request.CompanyId);
        var companyExists = await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return BadRequest("Selected company does not exist.");
        }

        var supplier = new Supplier(companyId, request.Code, request.Name, request.Phone, request.Email, request.Address);
        await dbContext.Suppliers.AddAsync(supplier, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, new SupplierDto(supplier.Id, supplier.CompanyId, null, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == ResolveCompanyId(null), cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        var companyId = ResolveCompanyId(request.CompanyId ?? supplier.CompanyId);
        var companyExists = await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists)
        {
            return BadRequest("Selected company does not exist.");
        }

        supplier.Update(companyId, request.Code, request.Name, request.Phone, request.Email, request.Address, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new SupplierDto(supplier.Id, supplier.CompanyId, null, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == ResolveCompanyId(null), cancellationToken);
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

    private Guid ResolveCompanyId(Guid? requestedCompanyId)
    {
        if (User.IsInRole(Roles.Admin) && requestedCompanyId is not null)
        {
            return requestedCompanyId.Value;
        }

        return currentUser.CompanyId ?? CompanyDefaults.DefaultCompanyId;
    }
}

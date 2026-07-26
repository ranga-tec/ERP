using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using ISS.Domain.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Procurement},{Roles.Finance}")]
public sealed class SuppliersController(IIssDbContext dbContext, ICurrentUser currentUser) : ControllerBase
{
    public sealed record SupplierDto(Guid Id, Guid CompanyId, string? CompanyCode, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive, bool IsAuthorized);
    public sealed record CreateSupplierRequest(Guid? CompanyId, string Code, string Name, string? Phone, string? Email, string? Address, bool? IsAuthorized);
    public sealed record UpdateSupplierRequest(Guid? CompanyId, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive, bool IsAuthorized);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var resolvedCompanyId = ResolveCompanyId(companyId);
        var query = dbContext.Suppliers.AsNoTracking().Where(x => x.CompanyId == resolvedCompanyId);

        var suppliers = await query
            .OrderBy(x => x.Code)
            .Select(x => new SupplierDto(x.Id, x.CompanyId, x.Company != null ? x.Company.Code : null, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive, x.IsAuthorized))
            .ToListAsync(cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.AsNoTracking()
            .Where(x => x.Id == id && x.CompanyId == ResolveCompanyId(null))
            .Select(x => new SupplierDto(x.Id, x.CompanyId, x.Company != null ? x.Company.Code : null, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive, x.IsAuthorized))
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
        supplier.Update(companyId, request.Code, request.Name, request.Phone, request.Email, request.Address, isActive: true, request.IsAuthorized ?? true);
        await dbContext.Suppliers.AddAsync(supplier, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, new SupplierDto(supplier.Id, supplier.CompanyId, null, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive, supplier.IsAuthorized));
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

        if (supplier.IsActive && !request.IsActive)
        {
            var blocker = await DescribeDeactivationBlockerAsync(id, cancellationToken);
            if (blocker is not null)
            {
                return BadRequest($"Cannot deactivate this supplier: {blocker}");
            }
        }

        supplier.Update(companyId, request.Code, request.Name, request.Phone, request.Email, request.Address, request.IsActive, request.IsAuthorized);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new SupplierDto(supplier.Id, supplier.CompanyId, null, supplier.Code, supplier.Name, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive, supplier.IsAuthorized));
    }

    /// <summary>
    /// Returns why the supplier must stay active, or null when it can be retired.
    /// Money still owed and procurement still in flight block; settled history does not.
    /// </summary>
    private async Task<string?> DescribeDeactivationBlockerAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var outstanding = await dbContext.AccountsPayableEntries.AsNoTracking()
            .Where(x => x.SupplierId == supplierId && x.Outstanding > 0)
            .SumAsync(x => (decimal?)x.Outstanding, cancellationToken) ?? 0m;
        if (outstanding > 0)
        {
            return $"{outstanding:0.00} still owed to them. Settle or write it off first.";
        }

        var openOrders = await dbContext.PurchaseOrders.AsNoTracking()
            .CountAsync(x => x.SupplierId == supplierId
                             && x.Status != PurchaseOrderStatus.Closed
                             && x.Status != PurchaseOrderStatus.Cancelled,
                cancellationToken);
        if (openOrders > 0)
        {
            return $"{openOrders} open purchase order(s) are still with them.";
        }

        // goods receipts reach the supplier through their purchase order
        var draftReceipts = await (
            from receipt in dbContext.GoodsReceipts.AsNoTracking()
            join order in dbContext.PurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.Id
            where order.SupplierId == supplierId && receipt.Status == GoodsReceiptStatus.Draft
            select receipt.Id).CountAsync(cancellationToken);
        if (draftReceipts > 0)
        {
            return $"{draftReceipts} draft goods receipt(s) reference them.";
        }

        return null;
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

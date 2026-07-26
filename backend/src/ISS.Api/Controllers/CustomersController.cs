using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using ISS.Domain.Sales;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Service},{Roles.Finance},{Roles.Inventory}")]
public sealed class CustomersController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record CustomerDto(Guid Id, string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);
    public sealed record CreateCustomerRequest(string Code, string Name, string? Phone, string? Email, string? Address);
    public sealed record UpdateCustomerRequest(string Code, string Name, string? Phone, string? Email, string? Address, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new CustomerDto(x.Id, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CustomerDto(x.Id, x.Code, x.Name, x.Phone, x.Email, x.Address, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Service},{Roles.Finance}")]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer(request.Code, request.Name, request.Phone, request.Email, request.Address);
        await dbContext.Customers.AddAsync(customer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, new CustomerDto(customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address, customer.IsActive));
    }

    /// <summary>
    /// Returns why the customer must stay active, or null when it can be retired.
    /// Money still owed and work still in flight block; settled history does not.
    /// </summary>
    private async Task<string?> DescribeDeactivationBlockerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var outstanding = await dbContext.AccountsReceivableEntries.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Outstanding > 0)
            .SumAsync(x => (decimal?)x.Outstanding, cancellationToken) ?? 0m;
        if (outstanding > 0)
        {
            return $"{outstanding:0.00} still outstanding on their account. Settle or write it off first.";
        }

        var openJobs = await dbContext.ServiceJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId
                             && x.Status != ServiceJobStatus.Closed
                             && x.Status != ServiceJobStatus.Cancelled,
                cancellationToken);
        if (openJobs > 0)
        {
            return $"{openJobs} service job(s) are still open for them.";
        }

        var openOrders = await dbContext.SalesOrders.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId
                             && x.Status != SalesOrderStatus.Closed
                             && x.Status != SalesOrderStatus.Cancelled,
                cancellationToken);
        if (openOrders > 0)
        {
            return $"{openOrders} open sales order(s) are still theirs.";
        }

        var liveUnits = await dbContext.EquipmentUnits.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.IsActive, cancellationToken);
        if (liveUnits > 0)
        {
            return $"{liveUnits} equipment unit(s) are still registered to them and may return for service.";
        }

        return null;
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Service},{Roles.Finance}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        if (customer.IsActive && !request.IsActive)
        {
            var blocker = await DescribeDeactivationBlockerAsync(id, cancellationToken);
            if (blocker is not null)
            {
                return BadRequest($"Cannot deactivate this customer: {blocker}");
            }
        }

        customer.Update(request.Code, request.Name, request.Phone, request.Email, request.Address, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerDto(customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address, customer.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Service},{Roles.Finance}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        dbContext.Customers.Remove(customer);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Customer is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

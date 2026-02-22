using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sales},{Roles.Service},{Roles.Finance}")]
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
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer(request.Code, request.Name, request.Phone, request.Email, request.Address);
        await dbContext.Customers.AddAsync(customer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, new CustomerDto(customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address, customer.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.Update(request.Code, request.Name, request.Phone, request.Email, request.Address, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerDto(customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address, customer.IsActive));
    }
}


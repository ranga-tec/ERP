using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/payment-types")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance}")]
public sealed class PaymentTypesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record PaymentTypeDto(Guid Id, string Code, string Name, string? Description, bool IsActive);
    public sealed record CreatePaymentTypeRequest(string Code, string Name, string? Description);
    public sealed record UpdatePaymentTypeRequest(string Code, string Name, string? Description, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentTypeDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.PaymentTypes.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new PaymentTypeDto(x.Id, x.Code, x.Name, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentTypeDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.PaymentTypes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PaymentTypeDto(x.Id, x.Code, x.Name, x.Description, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentTypeDto>> Create(CreatePaymentTypeRequest request, CancellationToken cancellationToken)
    {
        var item = new PaymentType(request.Code, request.Name, request.Description);
        await dbContext.PaymentTypes.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PaymentTypeDto>> Update(Guid id, UpdatePaymentTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.PaymentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.Code, request.Name, request.Description, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.PaymentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.PaymentTypes.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Payment type is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

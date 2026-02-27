using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/reference-forms")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Reporting},{Roles.Finance},{Roles.Inventory},{Roles.Procurement},{Roles.Sales},{Roles.Service}")]
public sealed class ReferenceFormsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ReferenceFormDto(Guid Id, string Code, string Name, string Module, string? RouteTemplate, bool IsActive);
    public sealed record CreateReferenceFormRequest(string Code, string Name, string Module, string? RouteTemplate);
    public sealed record UpdateReferenceFormRequest(string Code, string Name, string Module, string? RouteTemplate, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReferenceFormDto>>> List(CancellationToken cancellationToken)
    {
        var items = await dbContext.ReferenceForms.AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Code)
            .Select(x => new ReferenceFormDto(x.Id, x.Code, x.Name, x.Module, x.RouteTemplate, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReferenceFormDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ReferenceForms.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ReferenceFormDto(x.Id, x.Code, x.Name, x.Module, x.RouteTemplate, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ReferenceFormDto>> Create(CreateReferenceFormRequest request, CancellationToken cancellationToken)
    {
        var item = new ReferenceForm(request.Code, request.Name, request.Module, request.RouteTemplate);
        await dbContext.ReferenceForms.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(item.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReferenceFormDto>> Update(Guid id, UpdateReferenceFormRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ReferenceForms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Update(request.Code, request.Name, request.Module, request.RouteTemplate, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.ReferenceForms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.ReferenceForms.Remove(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Reference form is in use and cannot be deleted. Mark it inactive instead.");
        }

        return NoContent();
    }
}

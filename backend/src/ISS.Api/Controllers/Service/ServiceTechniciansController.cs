using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Service;

[ApiController]
[Route("api/service/technicians")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Service}")]
public sealed class ServiceTechniciansController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ServiceTechnicianDto(
        Guid Id,
        string Code,
        string Name,
        decimal DefaultCostRate,
        decimal DefaultBillingRate,
        string? Phone,
        string? Notes,
        bool IsActive);

    public sealed record CreateServiceTechnicianRequest(
        string Code,
        string Name,
        decimal DefaultCostRate,
        decimal DefaultBillingRate,
        string? Phone,
        string? Notes);

    public sealed record UpdateServiceTechnicianRequest(
        string Code,
        string Name,
        decimal DefaultCostRate,
        decimal DefaultBillingRate,
        string? Phone,
        string? Notes,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceTechnicianDto>>> List(CancellationToken cancellationToken)
    {
        var technicians = await dbContext.ServiceTechnicians.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ServiceTechnicianDto(
                x.Id,
                x.Code,
                x.Name,
                x.DefaultCostRate,
                x.DefaultBillingRate,
                x.Phone,
                x.Notes,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(technicians);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceTechnicianDto>> Create(CreateServiceTechnicianRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        var codeExists = await dbContext.ServiceTechnicians.AsNoTracking().AnyAsync(x => x.Code == code, cancellationToken);
        if (codeExists)
        {
            return Conflict("A technician with this code already exists.");
        }

        var technician = new ServiceTechnician(
            code,
            request.Name,
            request.DefaultCostRate,
            request.DefaultBillingRate,
            request.Phone,
            request.Notes);

        await dbContext.ServiceTechnicians.AddAsync(technician, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), new { id = technician.Id }, ToDto(technician));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceTechnicianDto>> Update(Guid id, UpdateServiceTechnicianRequest request, CancellationToken cancellationToken)
    {
        var technician = await dbContext.ServiceTechnicians.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (technician is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim();
        var codeExists = await dbContext.ServiceTechnicians.AsNoTracking().AnyAsync(x => x.Id != id && x.Code == code, cancellationToken);
        if (codeExists)
        {
            return Conflict("A technician with this code already exists.");
        }

        technician.Rename(code, request.Name);
        technician.Update(request.DefaultCostRate, request.DefaultBillingRate, request.Phone, request.Notes, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(technician));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var technician = await dbContext.ServiceTechnicians.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (technician is null)
        {
            return NotFound();
        }

        var used = await dbContext.WorkOrderTimeEntries.AsNoTracking().AnyAsync(x => x.TechnicianUserId == id, cancellationToken);
        if (used)
        {
            return Conflict("Technician is used by job detail labor entries. Mark inactive instead.");
        }

        dbContext.ServiceTechnicians.Remove(technician);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ServiceTechnicianDto ToDto(ServiceTechnician technician)
        => new(
            technician.Id,
            technician.Code,
            technician.Name,
            technician.DefaultCostRate,
            technician.DefaultBillingRate,
            technician.Phone,
            technician.Notes,
            technician.IsActive);
}

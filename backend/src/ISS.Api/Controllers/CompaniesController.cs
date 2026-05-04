using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Roles = Roles.AllBusiness)]
public sealed class CompaniesController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record CompanyDto(Guid Id, string Code, string Name, bool IsActive);
    public sealed record CreateCompanyRequest(string Code, string Name);
    public sealed record UpdateCompanyRequest(string Code, string Name, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> List(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Companies.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new CompanyDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CompanyDto(x.Id, x.Code, x.Name, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompanyDto>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = new Company(request.Code, request.Name);
        await dbContext.Companies.AddAsync(company, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = company.Id },
            new CompanyDto(company.Id, company.Code, company.Name, company.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompanyDto>> Update(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.Update(request.Code, request.Name, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CompanyDto(company.Id, company.Code, company.Name, company.IsActive));
    }
}

using ISS.Api.Security;
using ISS.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Finance;

[ApiController]
[Route("api/finance")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Finance},{Roles.Reporting}")]
public sealed class ArApController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record ArDto(Guid Id, Guid CustomerId, string ReferenceType, Guid ReferenceId, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);
    public sealed record ApDto(Guid Id, Guid SupplierId, string ReferenceType, Guid ReferenceId, decimal Amount, decimal Outstanding, DateTimeOffset PostedAt);

    [HttpGet("ar")]
    public async Task<ActionResult<IReadOnlyList<ArDto>>> ListAr([FromQuery] bool outstandingOnly = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountsReceivableEntries.AsNoTracking();
        if (outstandingOnly)
        {
            query = query.Where(x => x.Outstanding > 0);
        }

        var entries = await query
            .OrderByDescending(x => x.PostedAt)
            .Select(x => new ArDto(x.Id, x.CustomerId, x.ReferenceType, x.ReferenceId, x.Amount, x.Outstanding, x.PostedAt))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("ap")]
    public async Task<ActionResult<IReadOnlyList<ApDto>>> ListAp([FromQuery] bool outstandingOnly = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AccountsPayableEntries.AsNoTracking();
        if (outstandingOnly)
        {
            query = query.Where(x => x.Outstanding > 0);
        }

        var entries = await query
            .OrderByDescending(x => x.PostedAt)
            .Select(x => new ApDto(x.Id, x.SupplierId, x.ReferenceType, x.ReferenceId, x.Amount, x.Outstanding, x.PostedAt))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }
}


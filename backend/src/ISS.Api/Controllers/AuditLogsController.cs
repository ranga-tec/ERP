using ISS.Api.Security;
using ISS.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Reporting}")]
public sealed class AuditLogsController(IIssDbContext dbContext) : ControllerBase
{
    public sealed record AuditLogDto(Guid Id, DateTimeOffset OccurredAt, Guid? UserId, string TableName, int Action, string Key, string ChangesJson);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> List([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);

        var logs = await dbContext.AuditLogs.AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new AuditLogDto(x.Id, x.OccurredAt, x.UserId, x.TableName, (int)x.Action, x.Key, x.ChangesJson))
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}


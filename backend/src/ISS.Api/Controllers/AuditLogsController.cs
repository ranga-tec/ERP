using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Reporting}")]
public sealed class AuditLogsController(
    IIssDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private static readonly HashSet<string> TechnicalTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "DocumentSequences",
        "NotificationOutboxItems",
        "AssistantAccessPolicies",
        "AssistantProviderProfiles",
        "AssistantUserPreferences"
    };

    public sealed record AuditLogDto(
        Guid Id,
        DateTimeOffset OccurredAt,
        Guid? UserId,
        string? UserLabel,
        string TableName,
        string TableLabel,
        int Action,
        string Key,
        bool IsTechnical,
        string ChangesJson);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> List([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);

        var rawLogs = await dbContext.AuditLogs.AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new { x.Id, x.OccurredAt, x.UserId, x.TableName, x.Action, x.Key, x.ChangesJson })
            .ToListAsync(cancellationToken);

        var userIds = rawLogs
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToList();

        var userLabels = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await userManager.Users.AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    Label = !string.IsNullOrWhiteSpace(x.DisplayName)
                        ? x.DisplayName
                        : x.Email ?? x.UserName ?? x.Id.ToString()
                })
                .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);

        var logs = rawLogs
            .Select(x => new AuditLogDto(
                x.Id,
                x.OccurredAt,
                x.UserId,
                x.UserId is { } userId && userLabels.TryGetValue(userId, out var userLabel) ? userLabel : null,
                x.TableName,
                HumanizeIdentifier(x.TableName),
                (int)x.Action,
                x.Key,
                TechnicalTables.Contains(x.TableName),
                x.ChangesJson))
            .ToList();

        return Ok(logs);
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0
                && char.IsUpper(current)
                && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}

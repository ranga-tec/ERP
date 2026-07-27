using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Domain.MasterData;
using ISS.Domain.Service;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = $"{Roles.Admin}")]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IIssDbContext dbContext,
    AccessControlService accessControl) : ControllerBase
{
    public sealed record UserDto(
        Guid Id,
        Guid CompanyId,
        string? CompanyCode,
        string? CompanyName,
        string Email,
        string? DisplayName,
        bool IsLocked,
        DateTimeOffset? LockoutEnd,
        IReadOnlyList<string> Roles,
        string? TechnicianCode);

    public sealed record CreateUserRequest(Guid? CompanyId, string Email, string Password, string? DisplayName, string[] Roles);
    public sealed record SetCompanyRequest(Guid CompanyId);
    public sealed record SetRolesRequest(string[] Roles);
    public sealed record ResetPasswordRequest(string NewPassword);
    public sealed record PermissionDefinitionDto(string Key, string Module, string Action, string Label, string Description);
    public sealed record UserPermissionsDto(Guid UserId, bool HasExplicitOverrides, IReadOnlyList<string> EffectivePermissions);
    public sealed record SetUserPermissionsRequest(string[] Permissions);

    /// <summary>
    /// The service-technician profile attached to a staff login. Technicians are staff, so they
    /// are created here rather than entered a second time in the service module.
    /// </summary>
    public sealed record UserTechnicianProfileDto(
        Guid TechnicianId,
        string Code,
        string Name,
        decimal DefaultCostRate,
        decimal DefaultBillingRate,
        string? Phone,
        string? Notes,
        bool IsActive);

    public sealed record SaveUserTechnicianProfileRequest(
        string? Code,
        decimal DefaultCostRate,
        decimal DefaultBillingRate,
        string? Phone,
        string? Notes,
        bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var users = await userManager.Users.AsNoTracking()
            .OrderBy(x => x.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            dtos.Add(await ToDtoAsync(user));
        }

        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpGet("permission-definitions")]
    public ActionResult<IReadOnlyList<PermissionDefinitionDto>> PermissionDefinitions()
        => Ok(accessControl.Definitions
            .Select(x => new PermissionDefinitionDto(x.Key, x.Module, x.Action, x.Label, x.Description))
            .ToList());

    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<UserPermissionsDto>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var permissions = await accessControl.GetEffectivePermissionsAsync(id, cancellationToken);
        var hasExplicitOverrides = await accessControl.HasExplicitOverridesAsync(id, cancellationToken);
        return Ok(new UserPermissionsDto(id, hasExplicitOverrides, permissions.OrderBy(x => x).ToArray()));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult<UserPermissionsDto>> SetPermissions(Guid id, SetUserPermissionsRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            await accessControl.SetExplicitPermissionsAsync(id, request.Permissions, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var permissions = await accessControl.GetEffectivePermissionsAsync(id, cancellationToken);
        return Ok(new UserPermissionsDto(id, true, permissions.OrderBy(x => x).ToArray()));
    }

    [HttpDelete("{id:guid}/permissions")]
    public async Task<ActionResult<UserPermissionsDto>> ResetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await accessControl.ResetToRoleDefaultsAsync(id, cancellationToken);
        var permissions = await accessControl.GetEffectivePermissionsAsync(id, cancellationToken);
        return Ok(new UserPermissionsDto(id, false, permissions.OrderBy(x => x).ToArray()));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        await EnsureRolesExistAsync();

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { error = "Email is required." });
        }

        var roles = NormalizeRoles(request.Roles);
        if (!RolesAreValid(roles, out var invalid))
        {
            return BadRequest(new { error = $"Invalid role(s): {string.Join(", ", invalid)}" });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CompanyId = request.CompanyId ?? CompanyDefaults.DefaultCompanyId,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            LockoutEnabled = true
        };

        if (!await dbContext.Companies.AsNoTracking().AnyAsync(x => x.Id == user.CompanyId, HttpContext.RequestAborted))
        {
            return BadRequest(new { error = "Selected company does not exist." });
        }

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return BadRequest(new { errors = created.Errors.Select(e => e.Description).ToArray() });
        }

        if (roles.Length > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded)
            {
                return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description).ToArray() });
            }
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id:guid}/company")]
    public async Task<ActionResult<UserDto>> SetCompany(Guid id, SetCompanyRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var companyExists = await dbContext.Companies.AsNoTracking()
            .AnyAsync(x => x.Id == request.CompanyId, HttpContext.RequestAborted);
        if (!companyExists)
        {
            return BadRequest(new { error = "Selected company does not exist." });
        }

        user.CompanyId = request.CompanyId;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UserDto>> SetRoles(Guid id, SetRolesRequest request)
    {
        await EnsureRolesExistAsync();

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var desiredRoles = NormalizeRoles(request.Roles);
        if (!RolesAreValid(desiredRoles, out var invalid))
        {
            return BadRequest(new { error = $"Invalid role(s): {string.Join(", ", invalid)}" });
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Where(r => !desiredRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
        var toAdd = desiredRoles.Where(r => !currentRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();

        if (toRemove.Length > 0)
        {
            var remove = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!remove.Succeeded)
            {
                return BadRequest(new { errors = remove.Errors.Select(e => e.Description).ToArray() });
            }
        }

        if (toAdd.Length > 0)
        {
            var add = await userManager.AddToRolesAsync(user, toAdd);
            if (!add.Succeeded)
            {
                return BadRequest(new { errors = add.Errors.Select(e => e.Description).ToArray() });
            }
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
        {
            return BadRequest(new { errors = reset.Errors.Select(e => e.Description).ToArray() });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult> Disable(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.LockoutEnabled = true;
        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult> Enable(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.LockoutEnabled = true;
        var result = await userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/technician")]
    public async Task<ActionResult<UserTechnicianProfileDto?>> GetTechnicianProfile(Guid id, CancellationToken cancellationToken)
    {
        var technician = await dbContext.ServiceTechnicians.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);

        return technician is null ? Ok(null) : Ok(MapTechnician(technician));
    }

    /// <summary>Creates the technician profile for this user, or updates it if one already exists.</summary>
    [HttpPut("{id:guid}/technician")]
    public async Task<ActionResult<UserTechnicianProfileDto>> SaveTechnicianProfile(
        Guid id,
        SaveUserTechnicianProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var name = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName!
            : user.Email ?? user.UserName ?? id.ToString();

        var technician = await dbContext.ServiceTechnicians.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);

        if (technician is null)
        {
            var code = string.IsNullOrWhiteSpace(request.Code) ? await NextTechnicianCodeAsync(cancellationToken) : request.Code.Trim();
            if (await dbContext.ServiceTechnicians.AsNoTracking().AnyAsync(x => x.Code == code, cancellationToken))
            {
                return Conflict($"A technician with code '{code}' already exists.");
            }

            technician = new ServiceTechnician(
                code, name, request.DefaultCostRate, request.DefaultBillingRate,
                request.Phone, request.Notes, id);
            await dbContext.ServiceTechnicians.AddAsync(technician, cancellationToken);
        }
        else
        {
            var code = string.IsNullOrWhiteSpace(request.Code) ? technician.Code : request.Code.Trim();
            if (await dbContext.ServiceTechnicians.AsNoTracking().AnyAsync(x => x.Id != technician.Id && x.Code == code, cancellationToken))
            {
                return Conflict($"A technician with code '{code}' already exists.");
            }

            // keep the technician name following the staff login, so there is one place to rename
            technician.Rename(code, name);
            technician.Update(request.DefaultCostRate, request.DefaultBillingRate, request.Phone, request.Notes, request.IsActive);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(MapTechnician(technician));
    }

    private async Task<string> NextTechnicianCodeAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.ServiceTechnicians.AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var taken = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var n = 1; n < 10000; n++)
        {
            var candidate = $"TECH{n}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"TECH{Guid.NewGuid():N}"[..12];
    }

    private static UserTechnicianProfileDto MapTechnician(ServiceTechnician technician)
        => new(
            technician.Id,
            technician.Code,
            technician.Name,
            technician.DefaultCostRate,
            technician.DefaultBillingRate,
            technician.Phone,
            technician.Notes,
            technician.IsActive);

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var lockoutEnd = user.LockoutEnd;
        var isLocked = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow;
        var company = await dbContext.Companies.AsNoTracking()
            .Where(x => x.Id == user.CompanyId)
            .Select(x => new { x.Code, x.Name })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        var technicianCode = await dbContext.ServiceTechnicians.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        return new UserDto(
            user.Id,
            user.CompanyId,
            company?.Code,
            company?.Name,
            user.Email ?? user.UserName ?? string.Empty,
            user.DisplayName,
            isLocked,
            lockoutEnd,
            roles.OrderBy(x => x).ToArray(),
            technicianCode);
    }

    private static string[] NormalizeRoles(string[]? roles)
        => (roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool RolesAreValid(string[] roles, out string[] invalid)
    {
        invalid = roles.Where(r => !Roles.All.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
        return invalid.Length == 0;
    }

    private async Task EnsureRolesExistAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}

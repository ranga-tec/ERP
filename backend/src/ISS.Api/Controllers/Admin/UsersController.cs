using ISS.Api.Security;
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
    RoleManager<IdentityRole<Guid>> roleManager) : ControllerBase
{
    public sealed record UserDto(
        Guid Id,
        string Email,
        string? DisplayName,
        bool IsLocked,
        DateTimeOffset? LockoutEnd,
        IReadOnlyList<string> Roles);

    public sealed record CreateUserRequest(string Email, string Password, string? DisplayName, string[] Roles);
    public sealed record SetRolesRequest(string[] Roles);
    public sealed record ResetPasswordRequest(string NewPassword);

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
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            LockoutEnabled = true
        };

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

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var lockoutEnd = user.LockoutEnd;
        var isLocked = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow;

        return new UserDto(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.DisplayName,
            isLocked,
            lockoutEnd,
            roles.OrderBy(x => x).ToArray());
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


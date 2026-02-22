using ISS.Api.Security;
using ISS.Api.Services;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string? DisplayName);
    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var isFirstUser = !await userManager.Users.AnyAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName?.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });
        }

        if (isFirstUser)
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles);

        return Ok(new { token, userId = user.Id, email = user.Email, roles });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Unauthorized();
        }

        var ok = await userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles);
        return Ok(new { token, userId = user.Id, email = user.Email, roles });
    }
}


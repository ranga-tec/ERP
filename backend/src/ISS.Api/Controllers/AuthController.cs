using ISS.Api.Security;
using ISS.Api.Services;
using ISS.Application.Options;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    JwtTokenService jwtTokenService,
    IOptions<AuthOptions> authOptions,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string? DisplayName);
    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
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
        if (!IsRegistrationAllowed(isFirstUser))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Registration Disabled",
                    Detail = isFirstUser
                        ? "Initial bootstrap registration is disabled."
                        : "Self-registration is disabled. Contact an administrator to create your account."
                });
        }

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
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Unauthorized();
        }

        if (userManager.SupportsUserLockout)
        {
            if (!await userManager.GetLockoutEnabledAsync(user))
            {
                await userManager.SetLockoutEnabledAsync(user, true);
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Login Temporarily Locked",
                        Detail = "Too many failed login attempts. Try again later."
                    });
            }
        }

        var ok = await userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user);

                if (await userManager.IsLockedOutAsync(user))
                {
                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Login Temporarily Locked",
                            Detail = "Too many failed login attempts. Try again later."
                        });
                }
            }

            return Unauthorized();
        }

        if (userManager.SupportsUserLockout)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles);
        return Ok(new { token, userId = user.Id, email = user.Email, roles });
    }

    private bool IsRegistrationAllowed(bool isFirstUser)
    {
        var configured = authOptions.Value.AllowSelfRegistration;
        var allowSelfRegistration = configured ?? hostEnvironment.IsDevelopment();

        if (isFirstUser)
        {
            return authOptions.Value.AllowFirstUserBootstrapRegistration || allowSelfRegistration;
        }

        return allowSelfRegistration;
    }
}

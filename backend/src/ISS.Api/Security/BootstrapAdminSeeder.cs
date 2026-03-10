using ISS.Application.Options;
using ISS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ISS.Api.Security;

public static class BootstrapAdminSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        AuthOptions authOptions,
        ILogger logger)
    {
        var email = authOptions.BootstrapAdminEmail?.Trim();
        var password = authOptions.BootstrapAdminPassword?.Trim();
        var displayName = authOptions.BootstrapAdminDisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var details = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create bootstrap admin '{email}': {details}");
            }

            logger.LogWarning(
                "Bootstrap admin account created for {Email}. Remove Auth:BootstrapAdminPassword after first login if this account was intended for one-time recovery.",
                email);
        }
        else if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(user.DisplayName, displayName, StringComparison.Ordinal))
        {
            user.DisplayName = displayName;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
            logger.LogWarning("Bootstrap admin role granted to {Email}.", email);
        }
    }
}

using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Infrastructure.Documents;
using ISS.Infrastructure.Identity;
using ISS.Infrastructure.Notifications;
using ISS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ISS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIssInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string: ConnectionStrings:Default");
        }

        var enableRetryOnFailure = configuration.GetValue("Database:EnableRetryOnFailure", true);
        var maxRetryCount = Math.Clamp(configuration.GetValue("Database:MaxRetryCount", 5), 0, 20);
        var maxRetryDelaySeconds = Math.Clamp(configuration.GetValue("Database:MaxRetryDelaySeconds", 10), 1, 120);

        services.AddDbContext<IssDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (enableRetryOnFailure && maxRetryCount > 0)
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: maxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(maxRetryDelaySeconds),
                        errorCodesToAdd: null);
                }
            }));

        services.AddScoped<IIssDbContext>(sp => sp.GetRequiredService<IssDbContext>());
        services.AddScoped<IDocumentPdfService, DocumentPdfService>();

        services.AddHttpClient();
        services.Configure<SmtpEmailOptions>(configuration.GetSection("Notifications:Email:Smtp"));
        services.Configure<TwilioSmsOptions>(configuration.GetSection("Notifications:Sms:Twilio"));

        services.AddTransient<IEmailSender>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<SmtpEmailOptions>>().Value;
            return string.IsNullOrWhiteSpace(o.Host)
                ? ActivatorUtilities.CreateInstance<NullEmailSender>(sp)
                : ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
        });

        services.AddTransient<ISmsSender>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<TwilioSmsOptions>>().Value;
            return string.IsNullOrWhiteSpace(o.AccountSid)
                ? ActivatorUtilities.CreateInstance<NullSmsSender>(sp)
                : ActivatorUtilities.CreateInstance<TwilioSmsSender>(sp);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IssDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}

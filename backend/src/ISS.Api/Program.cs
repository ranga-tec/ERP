using ISS.Application;
using ISS.Application.Common;
using ISS.Application.Abstractions;
using ISS.Application.Options;
using ISS.Infrastructure;
using ISS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.RateLimiting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ISS.Api.Services.CurrentUser>();

builder.Services.AddIssApplication();
builder.Services.AddIssInfrastructure(builder.Configuration);
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<ReverseProxyOptions>(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.Configure<NotificationDispatcherOptions>(builder.Configuration.GetSection("Notifications:Dispatcher"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtIssuer = jwtSection["Issuer"] ?? "ISS";
        var jwtAudience = jwtSection["Audience"] ?? "ISS";
        var jwtKey = jwtSection["Key"] ?? "dev-only-change-me";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many requests. Please try again later."
            },
            cancellationToken: cancellationToken);
    };

    options.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetRateLimitClientKey(httpContext, "auth-login"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetRateLimitClientKey(httpContext, "auth-register"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddScoped<ISS.Api.Services.JwtTokenService>();
builder.Services.AddHostedService<ISS.Api.Services.NotificationDispatcherHostedService>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddCheck<ISS.Api.Health.DatabaseConnectivityHealthCheck>("database");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ISS API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var configuredJwtKey = builder.Configuration["Jwt:Key"];
JwtConfigurationValidator.ValidateSigningKeyOrThrow(configuredJwtKey, app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment() && JwtConfigurationValidator.UsesBuiltInDevelopmentFallback(configuredJwtKey))
{
    app.Logger.LogWarning(
        "Jwt:Key is not configured. Using the built-in development signing key. Set Jwt:Key for shared/dev deployments.");
}

var dbInitMode = (builder.Configuration["Database:InitializationMode"] ??
                  (app.Environment.IsDevelopment() ? "EnsureCreated" : "None"))
    .Trim();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IssDbContext>();
    switch (dbInitMode.ToLowerInvariant())
    {
        case "ensurecreated":
        case "ensure-created":
            app.Logger.LogInformation("Database initialization mode: EnsureCreated");
            await db.Database.EnsureCreatedAsync();
            break;

        case "migrate":
            app.Logger.LogInformation("Database initialization mode: Migrate");
            await db.Database.MigrateAsync();
            break;

        case "none":
            app.Logger.LogInformation("Database initialization mode: None (skipping automatic schema init)");
            break;

        default:
            throw new InvalidOperationException(
                $"Unsupported Database:InitializationMode '{dbInitMode}'. Expected EnsureCreated, Migrate, or None.");
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in ISS.Api.Security.Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

app.UseMiddleware<ISS.Api.Middleware.ExceptionHandlingMiddleware>();

var reverseProxy = app.Services.GetRequiredService<IOptions<ReverseProxyOptions>>().Value;
if (reverseProxy.Enabled)
{
    var forwardedHeadersOptions = BuildForwardedHeadersOptions(reverseProxy, app.Logger);
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();

static string GetRateLimitClientKey(HttpContext httpContext, string scope)
{
    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"{scope}:{ip}";
}

static ForwardedHeadersOptions BuildForwardedHeadersOptions(ReverseProxyOptions reverseProxy, ILogger logger)
{
    var options = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        RequireHeaderSymmetry = false,
    };

    if (reverseProxy.ForwardLimit is > 0)
    {
        options.ForwardLimit = reverseProxy.ForwardLimit.Value;
    }

    options.KnownProxies.Clear();
    foreach (var rawProxy in reverseProxy.KnownProxies)
    {
        var proxy = rawProxy?.Trim();
        if (string.IsNullOrWhiteSpace(proxy))
        {
            continue;
        }

        if (IPAddress.TryParse(proxy, out var parsedIp))
        {
            options.KnownProxies.Add(parsedIp);
        }
        else
        {
            logger.LogWarning("Ignoring invalid ReverseProxy:KnownProxies entry: {Proxy}", rawProxy);
        }
    }

    options.KnownNetworks.Clear();
    foreach (var rawNetwork in reverseProxy.KnownNetworks)
    {
        var network = rawNetwork?.Trim();
        if (string.IsNullOrWhiteSpace(network))
        {
            continue;
        }

        if (TryParseCidr(network, out var parsedNetwork))
        {
            options.KnownNetworks.Add(parsedNetwork);
        }
        else
        {
            logger.LogWarning("Ignoring invalid ReverseProxy:KnownNetworks entry: {Network}", rawNetwork);
        }
    }

    if (options.KnownProxies.Count == 0 && options.KnownNetworks.Count == 0)
    {
        logger.LogWarning(
            "ReverseProxy is enabled but no trusted proxies/networks are configured. Forwarded headers will be ignored until trusted sources are configured.");
    }

    logger.LogInformation(
        "Forwarded headers enabled. ForwardLimit={ForwardLimit}, KnownProxies={KnownProxyCount}, KnownNetworks={KnownNetworkCount}",
        options.ForwardLimit,
        options.KnownProxies.Count,
        options.KnownNetworks.Count);

    return options;
}

static bool TryParseCidr(string input, out Microsoft.AspNetCore.HttpOverrides.IPNetwork network)
{
    network = null!;

    var parts = input.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2)
    {
        return false;
    }

    if (!IPAddress.TryParse(parts[0], out var prefixAddress))
    {
        return false;
    }

    if (!int.TryParse(parts[1], out var prefixLength))
    {
        return false;
    }

    try
    {
        network = new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefixAddress, prefixLength);
        return true;
    }
    catch
    {
        return false;
    }
}

public partial class Program { }

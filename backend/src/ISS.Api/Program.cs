using ISS.Application;
using ISS.Application.Abstractions;
using ISS.Application.Options;
using ISS.Infrastructure;
using ISS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ISS.Api.Services.CurrentUser>();

builder.Services.AddIssApplication();
builder.Services.AddIssInfrastructure(builder.Configuration);
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

builder.Services.AddScoped<ISS.Api.Services.JwtTokenService>();
builder.Services.AddHostedService<ISS.Api.Services.NotificationDispatcherHostedService>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();

public partial class Program { }

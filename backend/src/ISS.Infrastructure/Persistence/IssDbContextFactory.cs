using ISS.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ISS.Infrastructure.Persistence;

public sealed class IssDbContextFactory : IDesignTimeDbContextFactory<IssDbContext>
{
    public IssDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString =
            Environment.GetEnvironmentVariable("ISS_EF_CONNECTION")
            ?? configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Unable to resolve a connection string for EF design-time operations. Set ISS_EF_CONNECTION or ConnectionStrings__Default.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<IssDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new IssDbContext(optionsBuilder.Options, new DesignTimeCurrentUser(), new DesignTimeClock());
    }

    private static IConfiguration BuildConfiguration()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "ISS.Api"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "ISS.Api"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "backend", "src", "ISS.Api")
        }
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        var configBasePath = candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")))
                             ?? Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(configBasePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
    }

    private sealed class DesignTimeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

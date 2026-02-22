using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ISS.Infrastructure.Persistence;

namespace ISS.IntegrationTests.Fixtures;

public sealed class IssApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Issuer"] = "ISS",
                ["Jwt:Audience"] = "ISS",
                ["Jwt:Key"] = "integration-tests-key-please-change",
                ["Notifications:Enabled"] = "true",
                ["Notifications:EmailEnabled"] = "true",
                ["Notifications:SmsEnabled"] = "true",
                ["Notifications:Dispatcher:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IssDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<IssDbContext>(options => options.UseNpgsql(connectionString));
        });
    }
}

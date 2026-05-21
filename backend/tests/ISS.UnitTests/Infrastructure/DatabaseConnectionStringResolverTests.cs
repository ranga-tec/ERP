using ISS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ISS.UnitTests.Infrastructure;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void Resolve_Prefers_Configured_Default_Connection_String()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=db;Database=iss;Username=app;Password=secret",
                ["DATABASE_URL"] = "postgres://railway:railway@railway.internal:5432/railway",
            })
            .Build();

        Assert.Equal("Host=db;Database=iss;Username=app;Password=secret", DatabaseConnectionStringResolver.Resolve(configuration));
    }

    [Fact]
    public void Resolve_Converts_Railway_Database_Url()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgresql://railway:p%40ss@postgres.railway.internal:5432/railway?sslmode=require",
            })
            .Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("postgres.railway.internal", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("railway", builder.Database);
        Assert.Equal("railway", builder.Username);
        Assert.Equal("p@ss", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void ConvertPostgresUrl_Rejects_Non_Postgres_Url()
    {
        Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringResolver.ConvertPostgresUrl("mysql://user:pass@localhost/db"));
    }
}

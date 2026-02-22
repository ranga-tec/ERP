using ISS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace ISS.IntegrationTests.Fixtures;

public sealed class IssApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private IssApiFactory? _factory;

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString => _postgres?.GetConnectionString() ?? throw new InvalidOperationException("Postgres not started.");
    public string AdminToken { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("iss_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        _factory = new IssApiFactory(_postgres.GetConnectionString());

        await EnsureDatabaseCreatedAsync(_factory);

        Client = _factory.CreateClient();
        AdminToken = await GetOrCreateAdminTokenAsync(Client);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    private static async Task EnsureDatabaseCreatedAsync(IssApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IssDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private static async Task<string> GetOrCreateAdminTokenAsync(HttpClient client)
    {
        var email = "admin@local";
        var password = "Passw0rd1";

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (login.IsSuccessStatusCode)
        {
            var data = await login.Content.ReadFromJsonAsync<AuthResponse>();
            return data!.Token;
        }

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password, displayName = "Admin" });
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        return registered!.Token;
    }

    private sealed record AuthResponse(string Token);
}

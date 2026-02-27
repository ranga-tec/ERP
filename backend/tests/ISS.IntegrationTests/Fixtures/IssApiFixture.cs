using ISS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace ISS.IntegrationTests.Fixtures;

public sealed class IssApiFixture : IAsyncLifetime
{
    private const string ExternalConnectionStringEnvVar = "ISS_INTEGRATIONTESTS_CONNECTION_STRING";
    private const string ResetExistingDatabaseEnvVar = "ISS_INTEGRATIONTESTS_RESET_EXISTING_DB";
    private const string HttpTimeoutSecondsEnvVar = "ISS_INTEGRATIONTESTS_HTTP_TIMEOUT_SECONDS";
    private const string DbReadyTimeoutSecondsEnvVar = "ISS_INTEGRATIONTESTS_DB_READY_TIMEOUT_SECONDS";
    private const int DefaultHttpTimeoutSeconds = 60;
    private const int DefaultDbReadyTimeoutSeconds = 60;

    private PostgreSqlContainer? _postgres;
    private IssApiFactory? _factory;
    private string? _connectionString;

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Test database connection is not initialized.");
    public string AdminToken { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var externalConnectionString = Environment.GetEnvironmentVariable(ExternalConnectionStringEnvVar)?.Trim();
        var usingExternalDatabase = !string.IsNullOrWhiteSpace(externalConnectionString);
        if (usingExternalDatabase)
        {
            _connectionString = externalConnectionString!;
        }
        else
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("iss_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
        }

        var dbReadyTimeoutSeconds = ReadPositiveIntEnvironmentVariable(DbReadyTimeoutSecondsEnvVar, DefaultDbReadyTimeoutSeconds);
        using (var dbReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(dbReadyTimeoutSeconds)))
        {
            await WaitForDatabaseReadyAsync(_connectionString, dbReadyCts.Token);
        }

        _factory = new IssApiFactory(_connectionString);

        var resetExistingDb = ReadBooleanEnvironmentVariable(ResetExistingDatabaseEnvVar);
        await EnsureDatabaseCreatedAsync(_factory, resetDatabase: !usingExternalDatabase || resetExistingDb);

        Client = _factory.CreateClient();
        var httpTimeoutSeconds = ReadPositiveIntEnvironmentVariable(HttpTimeoutSecondsEnvVar, DefaultHttpTimeoutSeconds);
        Client.Timeout = TimeSpan.FromSeconds(httpTimeoutSeconds);

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

    private static async Task EnsureDatabaseCreatedAsync(IssApiFactory factory, bool resetDatabase)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IssDbContext>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        if (resetDatabase)
        {
            await db.Database.EnsureDeletedAsync(cts.Token);
        }

        await db.Database.EnsureCreatedAsync(cts.Token);
    }

    private static bool ReadBooleanEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadPositiveIntEnvironmentVariable(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }

    private static async Task WaitForDatabaseReadyAsync(string connectionString, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await connection.CloseAsync();
                return;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
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

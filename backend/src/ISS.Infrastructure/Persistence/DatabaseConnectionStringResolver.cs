using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ISS.Infrastructure.Persistence;

public static class DatabaseConnectionStringResolver
{
    public static string? Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var databaseUrl = configuration["DATABASE_URL"];
        return string.IsNullOrWhiteSpace(databaseUrl)
            ? null
            : ConvertPostgresUrl(databaseUrl);
    }

    public static string ConvertPostgresUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql"))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be a PostgreSQL URL, for example postgres://user:password@host:5432/database.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        };

        if (userInfo.Length >= 1 && !string.IsNullOrWhiteSpace(userInfo[0]))
        {
            builder.Username = Uri.UnescapeDataString(userInfo[0]);
        }

        if (userInfo.Length == 2)
        {
            builder.Password = Uri.UnescapeDataString(userInfo[1]);
        }

        ApplyQueryParameters(uri.Query, builder);
        return builder.ConnectionString;
    }

    private static void ApplyQueryParameters(string query, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(value, ignoreCase: true);
                    break;
                case "application_name":
                case "applicationname":
                    builder.ApplicationName = value;
                    break;
                case "pooling":
                    if (bool.TryParse(value, out var pooling))
                    {
                        builder.Pooling = pooling;
                    }
                    break;
                case "timeout":
                    if (int.TryParse(value, out var timeout))
                    {
                        builder.Timeout = timeout;
                    }
                    break;
                case "commandtimeout":
                case "command_timeout":
                    if (int.TryParse(value, out var commandTimeout))
                    {
                        builder.CommandTimeout = commandTimeout;
                    }
                    break;
            }
        }
    }
}

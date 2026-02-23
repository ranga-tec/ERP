namespace ISS.Application.Common;

public static class JwtConfigurationValidator
{
    public const string BuiltInDevelopmentKey = "dev-only-change-me";
    public const int MinimumProductionKeyLength = 32;

    public static void ValidateSigningKeyOrThrow(string? configuredJwtKey, bool isDevelopment)
    {
        if (isDevelopment)
        {
            return;
        }

        var key = configuredJwtKey?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Key is required in non-Development environments.");
        }

        if (string.Equals(key, BuiltInDevelopmentKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Jwt:Key may not use the built-in development default in non-Development environments.");
        }

        if (key.Length < MinimumProductionKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumProductionKeyLength} characters in non-Development environments.");
        }
    }

    public static bool UsesBuiltInDevelopmentFallback(string? configuredJwtKey)
        => string.IsNullOrWhiteSpace(configuredJwtKey)
           || string.Equals(configuredJwtKey.Trim(), BuiltInDevelopmentKey, StringComparison.Ordinal);
}

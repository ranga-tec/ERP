using ISS.Application.Common;

namespace ISS.UnitTests.Application;

public sealed class JwtConfigurationValidatorTests
{
    [Fact]
    public void Development_Allows_Missing_Key()
    {
        JwtConfigurationValidator.ValidateSigningKeyOrThrow(configuredJwtKey: null, isDevelopment: true);
    }

    [Fact]
    public void Production_Missing_Key_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateSigningKeyOrThrow(configuredJwtKey: null, isDevelopment: false));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_BuiltIn_Default_Key_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateSigningKeyOrThrow(JwtConfigurationValidator.BuiltInDevelopmentKey, isDevelopment: false));

        Assert.Contains("built-in development default", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_Short_Key_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateSigningKeyOrThrow("short-key", isDevelopment: false));

        Assert.Contains("at least", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_Strong_Key_Passes()
    {
        JwtConfigurationValidator.ValidateSigningKeyOrThrow(
            configuredJwtKey: "0123456789abcdef0123456789abcdef",
            isDevelopment: false);
    }

    [Fact]
    public void UsesBuiltInDevelopmentFallback_Returns_True_For_Missing_Or_Default()
    {
        Assert.True(JwtConfigurationValidator.UsesBuiltInDevelopmentFallback(null));
        Assert.True(JwtConfigurationValidator.UsesBuiltInDevelopmentFallback("  "));
        Assert.True(JwtConfigurationValidator.UsesBuiltInDevelopmentFallback(JwtConfigurationValidator.BuiltInDevelopmentKey));
        Assert.False(JwtConfigurationValidator.UsesBuiltInDevelopmentFallback("0123456789abcdef0123456789abcdef"));
    }
}

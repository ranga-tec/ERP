using System.Diagnostics.CodeAnalysis;

namespace ISS.Domain.Common;

public static class Guard
{
    public static string NotNullOrWhiteSpace([NotNull] string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (maxLength is not null && trimmed.Length > maxLength.Value)
        {
            throw new DomainValidationException($"{fieldName} must be <= {maxLength.Value} characters.");
        }

        return trimmed;
    }

    public static decimal NotNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainValidationException($"{fieldName} cannot be negative.");
        }

        return value;
    }

    public static decimal Positive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainValidationException($"{fieldName} must be positive.");
        }

        return value;
    }
}


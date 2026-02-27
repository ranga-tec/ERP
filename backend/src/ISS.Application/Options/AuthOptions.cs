namespace ISS.Application.Options;

public sealed class AuthOptions
{
    // Null means environment default: enabled in Development, disabled in non-Development.
    public bool? AllowSelfRegistration { get; init; }

    // Allows initial bootstrap admin creation when the user table is empty.
    public bool AllowFirstUserBootstrapRegistration { get; init; } = true;
}


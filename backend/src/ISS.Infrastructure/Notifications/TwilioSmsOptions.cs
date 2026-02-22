namespace ISS.Infrastructure.Notifications;

public sealed class TwilioSmsOptions
{
    public string AccountSid { get; init; } = "";
    public string AuthToken { get; init; } = "";
    public string From { get; init; } = "";
}


namespace ISS.Application.Options;

public sealed class NotificationOptions
{
    public bool Enabled { get; init; } = false;
    public bool EmailEnabled { get; init; } = true;
    public bool SmsEnabled { get; init; } = true;
}


namespace ISS.Application.Options;

public sealed class NotificationDispatcherOptions
{
    public bool Enabled { get; init; } = false;
    public int PollSeconds { get; init; } = 10;
    public int BatchSize { get; init; } = 25;
    public int MaxAttempts { get; init; } = 5;
}


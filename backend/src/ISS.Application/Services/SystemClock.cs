using ISS.Application.Abstractions;

namespace ISS.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}


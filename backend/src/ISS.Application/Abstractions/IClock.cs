namespace ISS.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}


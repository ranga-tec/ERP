namespace ISS.Application.Options;

public sealed class ReverseProxyOptions
{
    public bool Enabled { get; init; } = false;
    public int? ForwardLimit { get; init; } = 1;
    public string[] KnownProxies { get; init; } = [];
    public string[] KnownNetworks { get; init; } = [];
}


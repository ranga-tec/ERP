namespace ISS.Infrastructure.Notifications;

public sealed class SmtpEmailOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseStartTls { get; init; } = true;
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "ISS ERP";
}


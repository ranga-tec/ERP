using ISS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ISS.Infrastructure.Notifications;

public sealed class NullSmsSender(ILogger<NullSmsSender> logger) : ISmsSender
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMS notifications disabled. Dropping SMS to {To}", message.To);
        return Task.CompletedTask;
    }
}


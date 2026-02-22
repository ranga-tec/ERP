using ISS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ISS.Infrastructure.Notifications;

public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email notifications disabled. Dropping email to {To} with subject {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}


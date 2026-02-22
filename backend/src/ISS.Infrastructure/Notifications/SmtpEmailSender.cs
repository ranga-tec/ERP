using ISS.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace ISS.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Host) || string.IsNullOrWhiteSpace(o.FromEmail))
        {
            throw new InvalidOperationException("SMTP email is not configured.");
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(o.FromName, o.FromEmail));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart(TextFormat.Plain) { Text = message.Body };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;

        try
        {
            var secureSocket = o.UseStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
            await client.ConnectAsync(o.Host, o.Port, secureSocket, cancellationToken);

            if (!string.IsNullOrWhiteSpace(o.User))
            {
                await client.AuthenticateAsync(o.User, o.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
        }
        finally
        {
            try
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SMTP disconnect failed.");
            }
        }
    }
}


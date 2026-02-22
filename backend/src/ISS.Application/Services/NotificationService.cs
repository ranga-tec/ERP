using ISS.Application.Abstractions;
using ISS.Application.Options;
using ISS.Application.Persistence;
using ISS.Domain.Notifications;
using Microsoft.Extensions.Options;

namespace ISS.Application.Services;

public sealed class NotificationService(
    IIssDbContext dbContext,
    IClock clock,
    IOptions<NotificationOptions> options)
{
    public bool Enabled => options.Value.Enabled;

    public void EnqueueEmail(string to, string subject, string body, string? referenceType = null, Guid? referenceId = null)
    {
        var o = options.Value;
        if (!o.Enabled || !o.EmailEnabled)
        {
            return;
        }

        dbContext.NotificationOutboxItems.Add(new NotificationOutboxItem(
            NotificationChannel.Email,
            to,
            subject,
            body,
            clock.UtcNow,
            referenceType,
            referenceId));
    }

    public void EnqueueSms(string to, string body, string? referenceType = null, Guid? referenceId = null)
    {
        var o = options.Value;
        if (!o.Enabled || !o.SmsEnabled)
        {
            return;
        }

        dbContext.NotificationOutboxItems.Add(new NotificationOutboxItem(
            NotificationChannel.Sms,
            to,
            subject: null,
            body,
            clock.UtcNow,
            referenceType,
            referenceId));
    }
}


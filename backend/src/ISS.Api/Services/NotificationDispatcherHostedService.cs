using ISS.Application.Abstractions;
using ISS.Application.Options;
using ISS.Application.Persistence;
using ISS.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ISS.Api.Services;

public sealed class NotificationDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDispatcherOptions> options,
    ILogger<NotificationDispatcherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var o = options.Value;
            if (!o.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, o.PollSeconds)), stoppingToken);
                continue;
            }

            try
            {
                await DispatchOnceAsync(o, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification dispatcher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, o.PollSeconds)), stoppingToken);
        }
    }

    private async Task DispatchOnceAsync(NotificationDispatcherOptions o, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIssDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var batchSize = Math.Clamp(o.BatchSize, 1, 500);

        var pending = await db.NotificationOutboxItems
            .Where(x => x.Status == NotificationStatus.Pending && x.NextAttemptAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var item in pending)
        {
            item.MarkProcessing(now);
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in pending)
        {
            try
            {
                switch (item.Channel)
                {
                    case NotificationChannel.Email:
                        await emailSender.SendAsync(new EmailMessage(item.Recipient, item.Subject ?? "Notification", item.Body), cancellationToken);
                        break;
                    case NotificationChannel.Sms:
                        await smsSender.SendAsync(new SmsMessage(item.Recipient, item.Body), cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported channel: {item.Channel}");
                }

                item.MarkSent(clock.UtcNow);
            }
            catch (Exception ex)
            {
                var delay = GetRetryDelay(item.Attempts);
                item.MarkFailed(clock.UtcNow, ex.ToString(), delay, maxAttempts: Math.Max(1, o.MaxAttempts));
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static TimeSpan GetRetryDelay(int attempts)
    {
        // Exponential backoff: 30s, 60s, 120s, ... capped at 1h.
        var exp = Math.Max(0, attempts - 1);
        var seconds = Math.Min(3600, 30 * Math.Pow(2, exp));
        return TimeSpan.FromSeconds(seconds);
    }
}


using ISS.Api.Security;
using ISS.Application.Abstractions;
using ISS.Application.Common;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Services;

public sealed class PettyCashCashCountReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PettyCashCashCountReminderHostedService> logger) : BackgroundService
{
    private const string DueReferenceType = "PCCC-DUE";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnqueueDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Petty-cash cash-count reminder iteration failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task EnqueueDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IIssDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var accessControl = scope.ServiceProvider.GetRequiredService<AccessControlService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var now = clock.UtcNow;

        var dueFunds = await dbContext.PettyCashFunds.AsNoTracking()
            .Where(fund => fund.IsActive
                           && fund.CashCountFrequency != PettyCashCashCountFrequency.None
                           && fund.NextCashCountDueAt != null
                           && fund.NextCashCountDueAt <= now)
            .Select(fund => new { fund.Id, fund.Code, fund.Name, fund.NextCashCountDueAt })
            .ToListAsync(cancellationToken);
        if (dueFunds.Count == 0)
        {
            return;
        }

        var recipients = await accessControl.GetActiveUserIdsWithAnyPermissionAsync(
            [AppPermissions.FinancePettyCashCashCountCreate],
            cancellationToken: cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        foreach (var fund in dueFunds)
        {
            var reminderAlreadySent = await dbContext.UserNotifications.AsNoTracking()
                .AnyAsync(
                    notification => notification.ReferenceType == DueReferenceType
                                    && notification.ReferenceId == fund.Id
                                    && notification.NotificationCreatedAt >= now.AddHours(-20),
                    cancellationToken);
            if (reminderAlreadySent)
            {
                continue;
            }

            notificationService.EnqueueInAppForUsers(
                recipients,
                "Petty cash count overdue",
                $"{fund.Code} - {fund.Name} was due for a cash count at {fund.NextCashCountDueAt:yyyy-MM-dd HH:mm} UTC.",
                $"/finance/petty-cash/{fund.Id}",
                DueReferenceType,
                fund.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

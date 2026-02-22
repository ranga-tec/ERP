using ISS.Application.Abstractions;
using ISS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIssApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<InventoryService>();
        services.AddScoped<InventoryOperationsService>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<ProcurementService>();
        services.AddScoped<SalesService>();
        services.AddScoped<ServiceManagementService>();
        services.AddScoped<FinanceService>();
        services.AddScoped<NotificationService>();
        return services;
    }
}

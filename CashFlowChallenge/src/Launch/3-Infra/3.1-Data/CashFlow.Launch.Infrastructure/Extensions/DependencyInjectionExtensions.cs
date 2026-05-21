using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;
using CashFlow.Launch.Domain.Services;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Launch.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddLaunchInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CashFlowDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<INotificator, Notificator>();
        services.AddScoped<IEntryRepository, EntryRepository>();

        return services;
    }
}
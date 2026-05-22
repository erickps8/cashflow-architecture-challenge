using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Consolidation.Domain.Notifications;
using CashFlow.Consolidation.Domain.Services;
using CashFlow.Consolidation.Infra.Context;
using CashFlow.Consolidation.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Consolidation.Infra.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConsolidationInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CashFlowConsolidationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<INotificator, Notificator>();
        services.AddScoped<IDailyConsolidationService, DailyConsolidationService>();
        services.AddScoped<IDailyConsolidationRepository, DailyConsolidationRepository>();

        return services;
    }
}
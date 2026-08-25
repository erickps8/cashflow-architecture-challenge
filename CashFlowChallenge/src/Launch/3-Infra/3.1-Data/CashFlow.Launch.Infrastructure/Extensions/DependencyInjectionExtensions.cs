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
    public static IServiceCollection AddLaunchInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CashFlowDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<INotificator, Notificator>();
        services.AddScoped<IEntryRepository, EntryRepository>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IRecurringEntryRepository, RecurringEntryRepository>();
        services.AddScoped<IRecurringEntryService, RecurringEntryService>();

        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICreditCardPurchaseRepository, CreditCardPurchaseRepository>();
        services.AddScoped<ICreditCardInstallmentRepository, CreditCardInstallmentRepository>();
        services.AddScoped<ICreditCardService, CreditCardService>();

        return services;
    }
}

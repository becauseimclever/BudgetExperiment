using BudgetExperiment.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Application;

/// <summary>
/// Application layer DI registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds application services.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Same collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<RecurringTransactionService>();
        services.AddScoped<RecurringTransferService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ICalendarGridService, CalendarGridService>();
        services.AddScoped<IPastDueService, PastDueService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IBalanceCalculationService, BalanceCalculationService>();
        return services;
    }
}

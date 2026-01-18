using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;

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
        services.AddScoped<IDayDetailService, DayDetailService>();
        services.AddScoped<ITransactionListService, TransactionListService>();
        services.AddScoped<IPastDueService, PastDueService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<IBalanceCalculationService, BalanceCalculationService>();
        services.AddScoped<IPaycheckAllocationService, PaycheckAllocationService>();
        services.AddScoped<IRecurringInstanceProjector, RecurringInstanceProjector>();
        services.AddScoped<IRecurringTransferInstanceProjector, RecurringTransferInstanceProjector>();
        services.AddScoped<IAutoRealizeService, AutoRealizeService>();
        services.AddScoped<IRecurringTransactionInstanceService, RecurringTransactionInstanceService>();
        services.AddScoped<IRecurringTransactionRealizationService, RecurringTransactionRealizationService>();
        services.AddScoped<IRecurringTransferInstanceService, RecurringTransferInstanceService>();
        services.AddScoped<IRecurringTransferRealizationService, RecurringTransferRealizationService>();
        services.AddScoped<IBudgetCategoryService, BudgetCategoryService>();
        services.AddScoped<IBudgetGoalService, BudgetGoalService>();
        services.AddScoped<IBudgetProgressService, BudgetProgressService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<ICategorizationEngine, CategorizationEngine>();
        services.AddScoped<ICategorizationRuleService, CategorizationRuleService>();
        return services;
    }
}

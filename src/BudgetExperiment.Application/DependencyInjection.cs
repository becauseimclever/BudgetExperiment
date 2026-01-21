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
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<TransactionService>(); // Also register concrete for backward compatibility
        services.AddScoped<CalendarService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<RecurringTransactionService>(); // Also register concrete for backward compatibility
        services.AddScoped<IRecurringTransferService, RecurringTransferService>();
        services.AddScoped<RecurringTransferService>(); // Also register concrete for backward compatibility
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
        services.AddScoped<IRuleSuggestionService, RuleSuggestionService>();
        services.AddScoped<IMerchantMappingService, MerchantMappingService>();
        services.AddScoped<ICategorySuggestionService, CategorySuggestionService>();
        services.AddScoped<ICsvParserService, CsvParserService>();
        services.AddScoped<IImportMappingService, ImportMappingService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<INaturalLanguageParser, NaturalLanguageParser>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ITransactionMatcher, TransactionMatcher>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}

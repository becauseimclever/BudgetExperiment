using System.Diagnostics.CodeAnalysis;

using BudgetExperiment.Application.Export;
using BudgetExperiment.Application.Settings;
using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Application;

/// <summary>
/// Application layer DI registration.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>Adds application services.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Same collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ICurrencyProvider, UserSettingsCurrencyProvider>();
        services.AddScoped<AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<IRecurringTransferService, RecurringTransferService>();
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
        services.AddScoped<IRecurringChargeDetectionService, RecurringChargeDetectionService>();
        services.AddScoped<IRecurringTransferInstanceService, RecurringTransferInstanceService>();
        services.AddScoped<IRecurringTransferRealizationService, RecurringTransferRealizationService>();
        services.AddScoped<IBudgetCategoryService, BudgetCategoryService>();
        services.AddScoped<IBudgetGoalService, BudgetGoalService>();
        services.AddScoped<IBudgetProgressService, BudgetProgressService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<ICategorizationEngine, CategorizationEngine>();
        services.AddScoped<ICategorizationRuleService, CategorizationRuleService>();
        services.AddScoped<IRuleSuggestionResponseParser, RuleSuggestionResponseParser>();
        services.AddScoped<ISuggestionAcceptanceHandler, SuggestionAcceptanceHandler>();
        services.AddScoped<IRuleSuggestionService, RuleSuggestionService>();
        services.AddScoped<IMerchantMappingService, MerchantMappingService>();
        services.AddScoped<ICategorySuggestionDismissalHandler, CategorySuggestionDismissalHandler>();
        services.AddScoped<ICategorySuggestionService, CategorySuggestionService>();
        services.AddScoped<ISuggestionMetricsService, SuggestionMetricsService>();
        services.AddScoped<IUncategorizedTransactionService, UncategorizedTransactionService>();
        services.AddScoped<IUnifiedTransactionService, UnifiedTransactionService>();
        services.AddScoped<IImportMappingService, ImportMappingService>();
        services.AddScoped<IImportDuplicateDetector, ImportDuplicateDetector>();
        services.AddScoped<IImportRowProcessor, ImportRowProcessor>();
        services.AddScoped<IImportPreviewEnricher, ImportPreviewEnricher>();
        services.AddScoped<IImportBatchManager, ImportBatchManager>();
        services.AddScoped<IImportTransactionCreator, ImportTransactionCreator>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<INaturalLanguageParser, NaturalLanguageParser>();
        services.AddScoped<IChatActionExecutor, ChatActionExecutor>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ITransactionMatcher, TransactionMatcher>();
        services.AddScoped<IReconciliationStatusBuilder, ReconciliationStatusBuilder>();
        services.AddScoped<IReconciliationMatchActionHandler, ReconciliationMatchActionHandler>();
        services.AddScoped<ILinkableInstanceFinder, LinkableInstanceFinder>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<ITrendReportBuilder, TrendReportBuilder>();
        services.AddScoped<ILocationReportBuilder, LocationReportBuilder>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICustomReportLayoutService, CustomReportLayoutService>();
        services.AddScoped<IExportFormatter, CsvExportService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ILocationParserService, LocationParserService>();
        return services;
    }
}

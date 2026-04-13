using BudgetExperiment.Application.Location;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.ExternalServices.AI;
using BudgetExperiment.Infrastructure.ExternalServices.Geocoding;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Infrastructure;

/// <summary>
/// DI extensions for infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds DbContext and repositories.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <returns>Same service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("AppDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Connection string 'AppDb' is required but was not found in configuration.");
        }

        // Bind DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<BudgetDbContext>(options => options.UseNpgsql(cs));

        // Register IUnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionQueryRepository, TransactionRepository>();
        services.AddScoped<ITransactionImportRepository, TransactionRepository>();
        services.AddScoped<ITransactionAnalyticsRepository, TransactionRepository>();
        services.AddScoped<IRecurringTransactionRepository, RecurringTransactionRepository>();
        services.AddScoped<IRecurringTransferRepository, RecurringTransferRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
        services.AddScoped<IBudgetGoalRepository, BudgetGoalRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<ICategorizationRuleRepository, CategorizationRuleRepository>();
        services.AddScoped<IRuleSuggestionRepository, RuleSuggestionRepository>();
        services.AddScoped<IImportMappingRepository, ImportMappingRepository>();
        services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IReconciliationMatchRepository, ReconciliationMatchRepository>();
        services.AddScoped<ICategorySuggestionRepository, CategorySuggestionRepository>();
        services.AddScoped<ILearnedMerchantMappingRepository, LearnedMerchantMappingRepository>();
        services.AddScoped<IDismissedSuggestionPatternRepository, DismissedSuggestionPatternRepository>();
        services.AddScoped<ICustomReportLayoutRepository, CustomReportLayoutRepository>();
        services.AddScoped<IRecurringChargeSuggestionRepository, RecurringChargeSuggestionRepository>();
        services.AddScoped<IReconciliationRecordRepository, ReconciliationRecordRepository>();
        services.AddScoped<IStatementBalanceRepository, StatementBalanceRepository>();
        services.AddScoped<IDismissedOutlierRepository, DismissedOutlierRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IMonthlyReflectionRepository, MonthlyReflectionRepository>();
        services.AddScoped<IKaizenGoalRepository, KaizenGoalRepository>();

        // AI services - backend selected at runtime from persisted settings, with config fallback.
        services.AddHttpClient<OllamaAiService>();
        services.AddHttpClient<LlamaCppAiService>();
        services.AddScoped<IAiService, BackendSelectingAiService>();

        // Geocoding Service - Nominatim (OpenStreetMap) reverse geocoding
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

        return services;
    }
}

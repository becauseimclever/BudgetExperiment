using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;
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

        // AI Settings Provider (database-backed)
        services.AddScoped<IAiSettingsProvider, AiSettingsProvider>();

        // AI Service - HttpClient configured dynamically from database settings
        services.AddHttpClient<IAiService, OllamaAiService>();

        return services;
    }
}

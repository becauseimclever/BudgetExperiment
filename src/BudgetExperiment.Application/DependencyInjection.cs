using BudgetExperiment.Application.AdhocTransactions;
using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Application.RecurringSchedules;
using BudgetExperiment.Application.RunningTotals;

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
        services.AddScoped<IRecurringScheduleService, RecurringScheduleService>();
        services.AddScoped<IAdhocTransactionService, AdhocTransactionService>();
        services.AddScoped<IRunningTotalService, RunningTotalService>();

        // CSV import services
        services.AddScoped<ICsvImportService, CsvImportService>();
        services.AddScoped<IBankCsvParser, BankOfAmericaCsvParser>();
        services.AddScoped<IBankCsvParser, CapitalOneCsvParser>();
        services.AddScoped<IBankCsvParser, UnitedHeritageCreditUnionCsvParser>();

        return services;
    }
}

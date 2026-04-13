// <copyright file="FeatureFlagSeeder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Seeding;

/// <summary>
/// Seeds default feature flags into the database. Never overwrites existing rows.
/// </summary>
public static class FeatureFlagSeeder
{
    private static readonly (string Name, bool IsEnabled)[] Defaults =
    [
        ("Calendar:SpendingHeatmap",               true),
        ("Calendar:KakeiboOverlay",                false),
        ("Kakeibo:TransactionOverride",            true),
        ("Kakeibo:TransactionFilter",              true),
        ("Kakeibo:MonthlyReflectionPrompts",       true),
        ("Kakeibo:CalendarOverlay",                false),
        ("Kaizen:MicroGoals",                      true),
        ("Kaizen:Dashboard",                       false),
        ("AI:ChatAssistant",                       true),
        ("AI:RuleSuggestions",                     true),
        ("AI:RecurringChargeDetection",            true),
        ("Reports:CustomReportBuilder",            false),
        ("Reports:LocationReport",                 true),
        ("Charts:AdvancedCharts",                  false),
        ("Charts:CandlestickChart",                false),
        ("Paycheck:PaycheckPlanner",               true),
        ("Reconciliation:StatementReconciliation", true),
        ("DataHealth:Dashboard",                   true),
        ("Location:Geocoding",                     false),
        ("Kakeibo:DateRangeReports",               false),
        ("feature-transfer-atomic-deletion",       false),
        ("feature-recurring-projection-accuracy",   false),
    ];

    /// <summary>
    /// Seeds any missing feature flags with their default values. Never overwrites existing rows.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task SeedAsync(BudgetDbContext context)
    {
        var now = DateTime.UtcNow;
        foreach (var (name, isEnabled) in Defaults)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "FeatureFlags" ("Name", "IsEnabled", "UpdatedAtUtc")
                VALUES ({0}, {1}, {2})
                ON CONFLICT ("Name") DO NOTHING
                """,
                name,
                isEnabled,
                now);
        }
    }
}

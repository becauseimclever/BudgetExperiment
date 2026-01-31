// <copyright file="NavigationHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Helper class with route constants for navigation testing.
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Primary navigation routes that appear in the main nav menu.
    /// </summary>
    public static readonly IReadOnlyList<(string Route, string Name)> PrimaryRoutes = new[]
    {
        (string.Empty, "Calendar"),
        ("recurring", "Recurring Bills"),
        ("recurring-transfers", "Auto-Transfers"),
        ("reconciliation", "Reconciliation"),
        ("transfers", "Transfers"),
        ("paycheck-planner", "Paycheck Planner"),
        ("categories", "Budget Categories"),
        ("rules", "Auto-Categorize"),
        ("budget", "Budget Overview"),
    };

    /// <summary>
    /// Secondary routes that may require expanding sections or are in the footer.
    /// </summary>
    public static readonly IReadOnlyList<(string Route, string Name)> SecondaryRoutes = new[]
    {
        ("reports", "Reports Overview"),
        ("reports/categories", "Category Spending"),
        ("accounts", "All Accounts"),
        ("import", "Import Transactions"),
        ("ai/suggestions", "Smart Insights"),
        ("category-suggestions", "Category Suggestions"),
        ("settings", "Settings"),
    };

    /// <summary>
    /// Gets all navigable routes in the application.
    /// </summary>
    public static IEnumerable<(string Route, string Name)> AllRoutes =>
        PrimaryRoutes.Concat(SecondaryRoutes);
}

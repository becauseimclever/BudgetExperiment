// <copyright file="IChartColorProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Provides colour resolution for chart categories and semantic transaction types.
/// All colours are sourced from the BudgetExperiment design system tokens.
/// </summary>
public interface IChartColorProvider
{
    /// <summary>
    /// Returns the hex colour for a given category name. Falls back to a
    /// deterministic position in the default palette when the category has no
    /// user-assigned colour, ensuring the same category always receives the
    /// same colour within a session.
    /// </summary>
    /// <param name="categoryName">The category name to resolve a colour for.</param>
    /// <returns>A non-empty hex colour string.</returns>
    string GetCategoryColor(string categoryName);

    /// <summary>
    /// Returns the full fallback colour palette used when no category colours are
    /// defined. Contains at least eight distinct, colorblind-aware colours.
    /// </summary>
    /// <returns>An array of hex colour strings.</returns>
    string[] GetDefaultPalette();

    /// <summary>
    /// Returns the semantic colour for income transactions
    /// (CSS <c>--color-income</c> baseline).
    /// </summary>
    /// <returns>A hex colour string.</returns>
    string GetIncomeColor();

    /// <summary>
    /// Returns the semantic colour for expense transactions
    /// (CSS <c>--color-expense</c> baseline).
    /// </summary>
    /// <returns>A hex colour string.</returns>
    string GetExpenseColor();

    /// <summary>
    /// Returns the semantic colour for transfer transactions
    /// (CSS <c>--color-transfer</c> baseline).
    /// </summary>
    /// <returns>A hex colour string.</returns>
    string GetTransferColor();
}

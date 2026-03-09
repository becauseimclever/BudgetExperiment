// <copyright file="IRuleSuggestionResponseParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Parses AI response JSON into <see cref="RuleSuggestion"/> domain objects.
/// </summary>
public interface IRuleSuggestionResponseParser
{
    /// <summary>
    /// Parses AI response JSON for new rule suggestions.
    /// </summary>
    /// <param name="jsonContent">The raw AI response text.</param>
    /// <param name="categories">Available budget categories for name-to-ID resolution.</param>
    /// <param name="transactionCount">Number of uncategorized transactions analyzed.</param>
    /// <returns>Parse result with suggestions and diagnostics.</returns>
    ParseResult<IReadOnlyList<RuleSuggestion>> ParseNewRuleSuggestions(
        string jsonContent,
        IReadOnlyList<BudgetCategory> categories,
        int transactionCount);

    /// <summary>
    /// Parses AI response JSON for optimization suggestions.
    /// </summary>
    /// <param name="jsonContent">The raw AI response text.</param>
    /// <param name="rules">Existing categorization rules for ID resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parse result with suggestions and diagnostics.</returns>
    Task<ParseResult<IReadOnlyList<RuleSuggestion>>> ParseOptimizationSuggestionsAsync(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct = default);

    /// <summary>
    /// Parses AI response JSON for conflict detection suggestions.
    /// </summary>
    /// <param name="jsonContent">The raw AI response text.</param>
    /// <param name="rules">Existing categorization rules for ID resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parse result with suggestions and diagnostics.</returns>
    Task<ParseResult<IReadOnlyList<RuleSuggestion>>> ParseConflictSuggestionsAsync(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct = default);
}

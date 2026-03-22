// <copyright file="RuleSuggestionAnalysis.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Result of comprehensive rule analysis.
/// </summary>
public sealed record RuleSuggestionAnalysis
{
    /// <summary>
    /// Gets suggestions for new categorization rules.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> NewRuleSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets suggestions for optimizing existing rules.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> OptimizationSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets suggestions about rule conflicts.
    /// </summary>
    public IReadOnlyList<RuleSuggestion> ConflictSuggestions { get; init; } = Array.Empty<RuleSuggestion>();

    /// <summary>
    /// Gets the number of uncategorized transactions analyzed.
    /// </summary>
    public int UncategorizedTransactionsAnalyzed
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of rules analyzed.
    /// </summary>
    public int RulesAnalyzed
    {
        get; init;
    }

    /// <summary>
    /// Gets the total duration of the analysis.
    /// </summary>
    public TimeSpan AnalysisDuration
    {
        get; init;
    }

    /// <summary>
    /// Gets diagnostic warnings from parsing AI responses (e.g., unknown categories, parse failures).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

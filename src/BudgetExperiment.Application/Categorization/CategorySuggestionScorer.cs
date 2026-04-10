// <copyright file="CategorySuggestionScorer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Scores category suggestion candidates based on transaction evidence
/// and ranks them by confidence and match count.
/// </summary>
public sealed class CategorySuggestionScorer : ICategorySuggestionScorer
{
    /// <inheritdoc />
    public decimal CalculateConfidence(int transactionCount, int patternCount)
    {
        var countConfidence = transactionCount switch
        {
            >= 10 => 0.9m,
            >= 5 => 0.8m,
            >= 3 => 0.7m,
            >= 2 => 0.6m,
            _ => 0.5m,
        };

        var patternBoost = patternCount switch
        {
            >= 3 => 0.1m,
            >= 2 => 0.05m,
            _ => 0m,
        };

        return Math.Min(0.99m, countConfidence + patternBoost);
    }

    /// <inheritdoc />
    public IReadOnlyList<CategorySuggestion> Rank(IEnumerable<CategorySuggestion> suggestions) =>
        suggestions
            .OrderByDescending(s => s.Confidence)
            .ThenByDescending(s => s.MatchingTransactionCount)
            .ToList();
}

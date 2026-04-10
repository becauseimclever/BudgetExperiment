// <copyright file="ICategorySuggestionScorer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Scores and ranks category suggestion candidates.
/// </summary>
public interface ICategorySuggestionScorer
{
    /// <summary>
    /// Calculates a confidence score for a suggestion based on transaction count and pattern diversity.
    /// </summary>
    /// <param name="transactionCount">The number of transactions that match the pattern.</param>
    /// <param name="patternCount">The number of distinct patterns supporting this category.</param>
    /// <returns>A confidence value in the range [0, 0.99].</returns>
    decimal CalculateConfidence(int transactionCount, int patternCount);

    /// <summary>
    /// Returns suggestions ordered by descending confidence, then by descending match count.
    /// </summary>
    /// <param name="suggestions">The unranked suggestions.</param>
    /// <returns>Ranked suggestions.</returns>
    IReadOnlyList<CategorySuggestion> Rank(IEnumerable<CategorySuggestion> suggestions);
}

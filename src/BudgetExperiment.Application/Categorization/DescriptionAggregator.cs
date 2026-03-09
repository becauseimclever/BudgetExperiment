// <copyright file="DescriptionAggregator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Aggregates transactions by cleaned description, producing frequency-ranked
/// groups annotated with occurrence counts and amount ranges for AI prompt enrichment.
/// </summary>
public static class DescriptionAggregator
{
    /// <summary>
    /// Default maximum number of description groups to return.
    /// </summary>
    public const int DefaultMaxGroups = 100;

    /// <summary>
    /// Cleans, deduplicates, and groups transaction descriptions by frequency.
    /// Returns at most <paramref name="maxGroups"/> groups ranked by descending frequency.
    /// </summary>
    /// <param name="transactions">The transactions to aggregate.</param>
    /// <param name="maxGroups">Maximum groups to return (default 100).</param>
    /// <returns>Description groups ranked by frequency descending.</returns>
    public static IReadOnlyList<DescriptionGroup> Aggregate(
        IReadOnlyList<Transaction> transactions,
        int maxGroups = DefaultMaxGroups)
    {
        if (transactions.Count == 0)
        {
            return Array.Empty<DescriptionGroup>();
        }

        return transactions
            .Select(t => new
            {
                Cleaned = TransactionDescriptionCleaner.Clean(t.Description),
                AbsAmount = Math.Abs(t.Amount.Amount),
            })
            .Where(x => !string.IsNullOrEmpty(x.Cleaned))
            .GroupBy(x => x.Cleaned, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DescriptionGroup(
                RepresentativeDescription: g.Key,
                Count: g.Count(),
                MinAmount: g.Min(x => x.AbsAmount),
                MaxAmount: g.Max(x => x.AbsAmount)))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.RepresentativeDescription, StringComparer.OrdinalIgnoreCase)
            .Take(maxGroups)
            .ToList();
    }
}

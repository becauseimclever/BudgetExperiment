// <copyright file="DescriptionSimilarityCalculator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Calculates similarity between transaction descriptions using normalization,
/// containment matching, and Levenshtein distance.
/// </summary>
public static class DescriptionSimilarityCalculator
{
    /// <summary>
    /// Calculates the similarity score between two descriptions.
    /// </summary>
    /// <param name="transactionDesc">The imported transaction description.</param>
    /// <param name="candidateDesc">The recurring instance description.</param>
    /// <returns>A decimal between 0 and 1 representing similarity.</returns>
    public static decimal CalculateSimilarity(string transactionDesc, string candidateDesc)
    {
        if (string.IsNullOrWhiteSpace(transactionDesc) || string.IsNullOrWhiteSpace(candidateDesc))
        {
            return 0m;
        }

        var normalizedTransaction = NormalizeDescription(transactionDesc);
        var normalizedCandidate = NormalizeDescription(candidateDesc);

        if (normalizedTransaction == normalizedCandidate)
        {
            return 1.0m;
        }

        if (normalizedTransaction.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.Contains(normalizedTransaction, StringComparison.OrdinalIgnoreCase))
        {
            var shorter = Math.Min(normalizedTransaction.Length, normalizedCandidate.Length);
            var longer = Math.Max(normalizedTransaction.Length, normalizedCandidate.Length);
            return (decimal)shorter / longer;
        }

        var distance = CalculateLevenshteinDistance(normalizedTransaction, normalizedCandidate);
        var maxLength = Math.Max(normalizedTransaction.Length, normalizedCandidate.Length);

        if (maxLength == 0)
        {
            return 1.0m;
        }

        var similarity = 1.0m - ((decimal)distance / maxLength);
        return Math.Max(0, similarity);
    }

    /// <summary>
    /// Normalizes a description by removing punctuation, collapsing whitespace, and uppercasing.
    /// </summary>
    /// <param name="description">The description to normalize.</param>
    /// <returns>The normalized description.</returns>
    public static string NormalizeDescription(string description)
    {
        var normalized = description
            .ToUpperInvariant()
            .Replace(".", string.Empty)
            .Replace(",", string.Empty)
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("*", string.Empty)
            .Replace("#", string.Empty);

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.Trim();
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var distance = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
        {
            distance[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            distance[0, j] = j;
        }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // deletion
                        distance[i, j - 1] + 1),     // insertion
                    distance[i - 1, j - 1] + cost);  // substitution
            }
        }

        return distance[sourceLength, targetLength];
    }
}

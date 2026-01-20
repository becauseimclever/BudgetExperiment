// <copyright file="TransactionMatcher.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Matches imported transactions with recurring transaction instances using
/// description similarity, amount tolerance, and date proximity scoring.
/// </summary>
public sealed class TransactionMatcher : ITransactionMatcher
{
    /// <summary>
    /// Weight for description similarity in overall confidence score.
    /// </summary>
    private const decimal DescriptionWeight = 0.50m;

    /// <summary>
    /// Weight for amount match in overall confidence score.
    /// </summary>
    private const decimal AmountWeight = 0.30m;

    /// <summary>
    /// Weight for date proximity in overall confidence score.
    /// </summary>
    private const decimal DateWeight = 0.20m;

    /// <summary>
    /// Confidence threshold for high level.
    /// </summary>
    private const decimal HighConfidenceThreshold = 0.85m;

    /// <summary>
    /// Confidence threshold for medium level.
    /// </summary>
    private const decimal MediumConfidenceThreshold = 0.60m;

    /// <inheritdoc/>
    public IReadOnlyList<TransactionMatchResult> FindMatches(
        Transaction transaction,
        IEnumerable<RecurringInstanceInfo> candidates,
        MatchingTolerances tolerances)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (tolerances is null)
        {
            throw new ArgumentNullException(nameof(tolerances));
        }

        var matches = new List<TransactionMatchResult>();

        foreach (var candidate in candidates)
        {
            var result = this.CalculateMatch(transaction, candidate, tolerances);
            if (result is not null)
            {
                matches.Add(result);
            }
        }

        return matches
            .OrderByDescending(m => m.ConfidenceScore)
            .ToList();
    }

    /// <inheritdoc/>
    public TransactionMatchResult? CalculateMatch(
        Transaction transaction,
        RecurringInstanceInfo candidate,
        MatchingTolerances tolerances)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (tolerances is null)
        {
            throw new ArgumentNullException(nameof(tolerances));
        }

        // Calculate date offset
        var dateOffsetDays = transaction.Date.DayNumber - candidate.InstanceDate.DayNumber;

        // Check date tolerance first (hard filter)
        if (Math.Abs(dateOffsetDays) > tolerances.DateToleranceDays)
        {
            return null;
        }

        // Calculate amount variance
        var actualAmount = transaction.Amount.Amount;
        var expectedAmount = candidate.Amount.Amount;
        var amountVariance = expectedAmount - actualAmount;

        // Check amount tolerance (hard filter - either percent or absolute must pass)
        if (!this.IsAmountWithinTolerance(actualAmount, expectedAmount, tolerances))
        {
            return null;
        }

        // Calculate description similarity
        var descriptionSimilarity = this.CalculateDescriptionSimilarity(
            transaction.Description,
            candidate.Description);

        // Check description threshold
        if (descriptionSimilarity < tolerances.DescriptionSimilarityThreshold)
        {
            return null;
        }

        // Calculate overall confidence score
        var dateScore = this.CalculateDateScore(dateOffsetDays, tolerances.DateToleranceDays);
        var amountScore = this.CalculateAmountScore(actualAmount, expectedAmount, tolerances);

        var confidenceScore =
            (descriptionSimilarity * DescriptionWeight) +
            (amountScore * AmountWeight) +
            (dateScore * DateWeight);

        var confidenceLevel = DetermineConfidenceLevel(confidenceScore);

        return new TransactionMatchResult(
            RecurringTransactionId: candidate.RecurringTransactionId,
            InstanceDate: candidate.InstanceDate,
            ConfidenceScore: confidenceScore,
            ConfidenceLevel: confidenceLevel,
            AmountVariance: amountVariance,
            DateOffsetDays: dateOffsetDays,
            DescriptionSimilarity: descriptionSimilarity);
    }

    private static MatchConfidenceLevel DetermineConfidenceLevel(decimal confidenceScore)
    {
        if (confidenceScore >= HighConfidenceThreshold)
        {
            return MatchConfidenceLevel.High;
        }

        if (confidenceScore >= MediumConfidenceThreshold)
        {
            return MatchConfidenceLevel.Medium;
        }

        return MatchConfidenceLevel.Low;
    }

    private bool IsAmountWithinTolerance(
        decimal actualAmount,
        decimal expectedAmount,
        MatchingTolerances tolerances)
    {
        var difference = Math.Abs(actualAmount - expectedAmount);

        // Check absolute tolerance
        if (difference <= tolerances.AmountToleranceAbsolute)
        {
            return true;
        }

        // Check percent tolerance
        if (expectedAmount != 0)
        {
            var percentDifference = difference / Math.Abs(expectedAmount);
            if (percentDifference <= tolerances.AmountTolerancePercent)
            {
                return true;
            }
        }
        else if (actualAmount == 0)
        {
            // Both are zero, within tolerance
            return true;
        }

        return false;
    }

    private decimal CalculateDateScore(int dateOffsetDays, int maxToleranceDays)
    {
        if (maxToleranceDays == 0)
        {
            return dateOffsetDays == 0 ? 1.0m : 0.0m;
        }

        // Linear decay: closer to scheduled date = higher score
        var normalizedOffset = (decimal)Math.Abs(dateOffsetDays) / maxToleranceDays;
        return Math.Max(0, 1.0m - normalizedOffset);
    }

    private decimal CalculateAmountScore(
        decimal actualAmount,
        decimal expectedAmount,
        MatchingTolerances tolerances)
    {
        var difference = Math.Abs(actualAmount - expectedAmount);

        if (difference == 0)
        {
            return 1.0m;
        }

        // Calculate how close we are within tolerance
        if (expectedAmount != 0)
        {
            var percentDiff = difference / Math.Abs(expectedAmount);
            var absoluteDiff = difference;

            // Use whichever tolerance gives us a better score
            var percentScore = tolerances.AmountTolerancePercent > 0
                ? 1.0m - Math.Min(1.0m, percentDiff / tolerances.AmountTolerancePercent)
                : 0m;

            var absoluteScore = tolerances.AmountToleranceAbsolute > 0
                ? 1.0m - Math.Min(1.0m, absoluteDiff / tolerances.AmountToleranceAbsolute)
                : 0m;

            return Math.Max(percentScore, absoluteScore);
        }

        // Expected is zero, use absolute tolerance
        return tolerances.AmountToleranceAbsolute > 0
            ? 1.0m - Math.Min(1.0m, difference / tolerances.AmountToleranceAbsolute)
            : 0m;
    }

    private decimal CalculateDescriptionSimilarity(string transactionDesc, string candidateDesc)
    {
        if (string.IsNullOrWhiteSpace(transactionDesc) || string.IsNullOrWhiteSpace(candidateDesc))
        {
            return 0m;
        }

        // Normalize both strings
        var normalizedTransaction = NormalizeDescription(transactionDesc);
        var normalizedCandidate = NormalizeDescription(candidateDesc);

        if (normalizedTransaction == normalizedCandidate)
        {
            return 1.0m;
        }

        // Check if one contains the other (common with bank descriptions that add extra info)
        if (normalizedTransaction.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.Contains(normalizedTransaction, StringComparison.OrdinalIgnoreCase))
        {
            // Higher score for longer matches
            var shorter = Math.Min(normalizedTransaction.Length, normalizedCandidate.Length);
            var longer = Math.Max(normalizedTransaction.Length, normalizedCandidate.Length);
            return (decimal)shorter / longer;
        }

        // Use Levenshtein distance for fuzzy matching
        var distance = this.CalculateLevenshteinDistance(normalizedTransaction, normalizedCandidate);
        var maxLength = Math.Max(normalizedTransaction.Length, normalizedCandidate.Length);

        if (maxLength == 0)
        {
            return 1.0m;
        }

        var similarity = 1.0m - ((decimal)distance / maxLength);
        return Math.Max(0, similarity);
    }

    private static string NormalizeDescription(string description)
    {
        // Remove common noise, normalize whitespace, lowercase
        var normalized = description
            .ToUpperInvariant()
            .Replace(".", string.Empty)
            .Replace(",", string.Empty)
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("*", string.Empty)
            .Replace("#", string.Empty);

        // Normalize multiple spaces to single space
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.Trim();
    }

    private int CalculateLevenshteinDistance(string source, string target)
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

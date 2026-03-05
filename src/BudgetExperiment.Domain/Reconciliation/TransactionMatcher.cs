// <copyright file="TransactionMatcher.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

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

    /// <summary>
    /// High confidence score for import pattern matches.
    /// </summary>
    private const decimal PatternMatchConfidence = 0.98m;

    /// <inheritdoc/>
    public IReadOnlyList<TransactionMatchResultValue> FindMatches(
        Transaction transaction,
        IEnumerable<RecurringInstanceInfoValue> candidates,
        MatchingTolerancesValue tolerances)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(tolerances);

        return candidates
            .Select(c => this.CalculateMatch(transaction, c, tolerances))
            .Where(r => r is not null)
            .OrderByDescending(r => r!.ConfidenceScore)
            .ToList()!;
    }

    /// <inheritdoc/>
    public TransactionMatchResultValue? CalculateMatch(
        Transaction transaction,
        RecurringInstanceInfoValue candidate,
        MatchingTolerancesValue tolerances)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(tolerances);

        var dateOffsetDays = transaction.Date.DayNumber - candidate.InstanceDate.DayNumber;
        var actualAmount = transaction.Amount.Amount;
        var expectedAmount = candidate.Amount.Amount;

        // Hard filters: date tolerance, amount tolerance, description similarity
        if (!this.PassesHardFilters(
                dateOffsetDays,
                actualAmount,
                expectedAmount,
                transaction.Description,
                candidate,
                tolerances,
                out var hasPatternMatch,
                out var descriptionSimilarity))
        {
            return null;
        }

        var confidenceScore = this.CalculateOverallConfidence(
            hasPatternMatch, descriptionSimilarity, dateOffsetDays, actualAmount, expectedAmount, tolerances);

        return new TransactionMatchResultValue(
            RecurringTransactionId: candidate.RecurringTransactionId,
            InstanceDate: candidate.InstanceDate,
            ConfidenceScore: confidenceScore,
            ConfidenceLevel: DetermineConfidenceLevel(confidenceScore),
            AmountVariance: expectedAmount - actualAmount,
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

    private bool PassesHardFilters(
        int dateOffsetDays,
        decimal actualAmount,
        decimal expectedAmount,
        string transactionDescription,
        RecurringInstanceInfoValue candidate,
        MatchingTolerancesValue tolerances,
        out bool hasPatternMatch,
        out decimal descriptionSimilarity)
    {
        hasPatternMatch = false;
        descriptionSimilarity = 0m;

        if (Math.Abs(dateOffsetDays) > tolerances.DateToleranceDays)
        {
            return false;
        }

        if (!this.IsAmountWithinTolerance(actualAmount, expectedAmount, tolerances))
        {
            return false;
        }

        hasPatternMatch = this.MatchesImportPatterns(transactionDescription, candidate.ImportPatterns);
        descriptionSimilarity = hasPatternMatch
            ? 1.0m
            : DescriptionSimilarityCalculator.CalculateSimilarity(transactionDescription, candidate.Description);

        return hasPatternMatch || descriptionSimilarity >= tolerances.DescriptionSimilarityThreshold;
    }

    private decimal CalculateOverallConfidence(
        bool hasPatternMatch,
        decimal descriptionSimilarity,
        int dateOffsetDays,
        decimal actualAmount,
        decimal expectedAmount,
        MatchingTolerancesValue tolerances)
    {
        var dateScore = this.CalculateDateScore(dateOffsetDays, tolerances.DateToleranceDays);
        var amountScore = this.CalculateAmountScore(actualAmount, expectedAmount, tolerances);

        if (hasPatternMatch)
        {
            return PatternMatchConfidence * ((((dateScore + amountScore) / 2) * 0.02m) + 0.98m);
        }

        return (descriptionSimilarity * DescriptionWeight) +
               (amountScore * AmountWeight) +
               (dateScore * DateWeight);
    }

    private bool MatchesImportPatterns(string transactionDescription, IReadOnlyCollection<ImportPatternValue>? patterns)
    {
        if (patterns is null || patterns.Count == 0)
        {
            return false;
        }

        return patterns.Any(p => p.Matches(transactionDescription));
    }

    private bool IsAmountWithinTolerance(
        decimal actualAmount,
        decimal expectedAmount,
        MatchingTolerancesValue tolerances)
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
        MatchingTolerancesValue tolerances)
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
}

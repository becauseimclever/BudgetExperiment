// <copyright file="RecurrenceDetector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Pure domain logic for detecting recurring charge patterns from transaction history.
/// Groups transactions by normalized description, detects regular intervals, and scores confidence.
/// </summary>
public static class RecurrenceDetector
{
    private const decimal IntervalRegularityWeight = 0.40m;
    private const decimal AmountConsistencyWeight = 0.30m;
    private const decimal SampleSizeWeight = 0.20m;
    private const decimal RecencyWeight = 0.10m;
    private const int SampleSizeCap = 12;

    /// <summary>
    /// Detects recurring charge patterns from a list of transactions.
    /// Transactions already linked to a recurring transaction are excluded.
    /// </summary>
    /// <param name="transactions">The transactions to analyze.</param>
    /// <param name="options">Detection configuration options.</param>
    /// <param name="today">The reference date for recency scoring (defaults to today).</param>
    /// <returns>A list of detected patterns sorted by confidence descending.</returns>
    public static IReadOnlyList<DetectedPattern> Detect(
        IReadOnlyList<TransactionSnapshot> transactions,
        RecurrenceDetectionOptions options,
        DateOnly? today = null)
    {
        var referenceDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var results = new List<DetectedPattern>();

        // Group by normalized description
        var groups = transactions
            .Where(t => !t.RecurringTransactionId.HasValue)
            .Where(t => !string.IsNullOrWhiteSpace(t.Description))
            .GroupBy(t => DescriptionNormalizer.Normalize(t.Description))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key));

        foreach (var group in groups)
        {
            var sorted = group.OrderBy(t => t.Date).ToList();

            if (sorted.Count < options.MinimumOccurrences)
            {
                continue;
            }

            // Check amount consistency
            var amounts = sorted.Select(t => t.Amount).ToList();
            var averageAmount = amounts.Average();

            if (averageAmount == 0m)
            {
                continue;
            }

            if (!IsAmountConsistent(amounts, averageAmount, options.AmountVarianceTolerance))
            {
                continue;
            }

            // Detect frequency
            var dates = sorted.Select(t => t.Date).ToList();
            var (frequency, interval) = DetectFrequency(dates);

            if (frequency is null)
            {
                continue;
            }

            // Score confidence
            var confidence = CalculateConfidence(
                dates,
                amounts,
                averageAmount,
                frequency.Value,
                interval,
                referenceDate);

            if (confidence < options.MinimumConfidence)
            {
                continue;
            }

            // Find the most commonly used currency
            var currency = sorted
                .GroupBy(t => t.Currency)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            // Find the most commonly used category
            var mostUsedCategoryId = sorted
                .Where(t => t.CategoryId.HasValue)
                .GroupBy(t => t.CategoryId!.Value)
                .OrderByDescending(g => g.Count())
                .Select(g => (Guid?)g.Key)
                .FirstOrDefault();

            results.Add(new DetectedPattern(
                NormalizedDescription: group.Key,
                SampleDescription: sorted[0].Description,
                AverageAmount: MoneyValue.Create(currency, Math.Round(averageAmount, 2, MidpointRounding.AwayFromZero)),
                Frequency: frequency.Value,
                Interval: interval,
                Confidence: confidence,
                MatchingTransactionIds: sorted.Select(t => t.Id).ToList(),
                FirstOccurrence: dates[0],
                LastOccurrence: dates[^1],
                MostUsedCategoryId: mostUsedCategoryId));
        }

        return results.OrderByDescending(p => p.Confidence).ToList();
    }

    private static bool IsAmountConsistent(
        List<decimal> amounts,
        decimal average,
        decimal tolerance)
    {
        var absAverage = Math.Abs(average);
        if (absAverage < 0.01m)
        {
            return false;
        }

        return amounts.All(a => Math.Abs(a - average) / absAverage <= tolerance);
    }

    private static (RecurrenceFrequency? Frequency, int Interval) DetectFrequency(List<DateOnly> dates)
    {
        if (dates.Count < 2)
        {
            return (null, 0);
        }

        var intervals = new List<int>();
        for (int i = 1; i < dates.Count; i++)
        {
            intervals.Add(dates[i].DayNumber - dates[i - 1].DayNumber);
        }

        var medianDays = GetMedian(intervals);

        return medianDays switch
        {
            >= 5 and <= 9 => (RecurrenceFrequency.Weekly, 1),
            >= 12 and <= 16 => (RecurrenceFrequency.BiWeekly, 1),
            >= 26 and <= 35 => (RecurrenceFrequency.Monthly, 1),
            >= 55 and <= 70 => (RecurrenceFrequency.Monthly, 2),
            >= 80 and <= 100 => (RecurrenceFrequency.Quarterly, 1),
            >= 170 and <= 200 => (RecurrenceFrequency.Quarterly, 2),
            >= 350 and <= 380 => (RecurrenceFrequency.Yearly, 1),
            _ => (null, 0),
        };
    }

    private static decimal CalculateConfidence(
        List<DateOnly> dates,
        List<decimal> amounts,
        decimal averageAmount,
        RecurrenceFrequency frequency,
        int interval,
        DateOnly referenceDate)
    {
        // 1. Interval regularity (40%) — how consistent are the gaps?
        var intervalRegularity = CalculateIntervalRegularity(dates, frequency, interval);

        // 2. Amount consistency (30%) — coefficient of variation
        var amountConsistency = CalculateAmountConsistency(amounts, averageAmount);

        // 3. Sample size (20%) — more occurrences = higher confidence, capped at 12
        var sampleSizeScore = Math.Min((decimal)dates.Count / SampleSizeCap, 1.0m);

        // 4. Recency (10%) — bonus if last occurrence is within one expected interval of today
        var recencyScore = CalculateRecencyScore(dates[^1], frequency, interval, referenceDate);

        var confidence =
            (intervalRegularity * IntervalRegularityWeight) +
            (amountConsistency * AmountConsistencyWeight) +
            (sampleSizeScore * SampleSizeWeight) +
            (recencyScore * RecencyWeight);

        return Math.Round(Math.Min(confidence, 1.0m), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateIntervalRegularity(
        List<DateOnly> dates,
        RecurrenceFrequency frequency,
        int interval)
    {
        var expectedDays = GetExpectedDays(frequency, interval);
        if (expectedDays == 0 || dates.Count < 2)
        {
            return 0m;
        }

        var deviations = new List<decimal>();
        for (int i = 1; i < dates.Count; i++)
        {
            var actualDays = dates[i].DayNumber - dates[i - 1].DayNumber;
            var deviation = Math.Abs(actualDays - expectedDays) / (decimal)expectedDays;
            deviations.Add(deviation);
        }

        var avgDeviation = deviations.Average();

        // Convert deviation to a 0–1 score (0% deviation = 1.0, 30%+ deviation = 0.0)
        return Math.Max(0m, 1.0m - (avgDeviation / 0.30m));
    }

    private static decimal CalculateAmountConsistency(List<decimal> amounts, decimal average)
    {
        if (amounts.Count < 2 || average == 0m)
        {
            return 0m;
        }

        var absAverage = Math.Abs(average);
        var sumSquaredDiffs = amounts.Sum(a => (a - average) * (a - average));
        var standardDeviation = (decimal)Math.Sqrt((double)(sumSquaredDiffs / amounts.Count));
        var coefficientOfVariation = standardDeviation / absAverage;

        // CV of 0 = perfect consistency (1.0), CV of 0.10+ = (0.0)
        return Math.Max(0m, 1.0m - (coefficientOfVariation / 0.10m));
    }

    private static decimal CalculateRecencyScore(
        DateOnly lastOccurrence,
        RecurrenceFrequency frequency,
        int interval,
        DateOnly referenceDate)
    {
        var expectedDays = GetExpectedDays(frequency, interval);
        if (expectedDays == 0)
        {
            return 0m;
        }

        var daysSinceLast = referenceDate.DayNumber - lastOccurrence.DayNumber;

        // Full score if within expected interval, linear decay to 0 over 2x expected interval
        if (daysSinceLast <= expectedDays)
        {
            return 1.0m;
        }

        if (daysSinceLast <= expectedDays * 2)
        {
            return 1.0m - ((decimal)(daysSinceLast - expectedDays) / expectedDays);
        }

        return 0m;
    }

    private static int GetExpectedDays(RecurrenceFrequency frequency, int interval) =>
        frequency switch
        {
            RecurrenceFrequency.Weekly => 7 * interval,
            RecurrenceFrequency.BiWeekly => 14,
            RecurrenceFrequency.Monthly => 30 * interval,
            RecurrenceFrequency.Quarterly => 91,
            RecurrenceFrequency.Yearly => 365,
            _ => 0,
        };

    private static int GetMedian(List<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;
        if (count == 0)
        {
            return 0;
        }

        return count % 2 == 0
            ? (sorted[(count / 2) - 1] + sorted[count / 2]) / 2
            : sorted[count / 2];
    }
}

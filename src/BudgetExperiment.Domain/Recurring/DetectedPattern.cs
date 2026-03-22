// <copyright file="DetectedPattern.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Represents a detected recurring charge pattern from transaction analysis.
/// </summary>
/// <param name="NormalizedDescription">The normalized description used for grouping.</param>
/// <param name="SampleDescription">An original description for display purposes.</param>
/// <param name="AverageAmount">The average monetary amount across matching transactions.</param>
/// <param name="Frequency">The detected recurrence frequency.</param>
/// <param name="Interval">The interval for the frequency (e.g. 1 for monthly, 2 for every-other-month).</param>
/// <param name="Confidence">The confidence score (0.0–1.0).</param>
/// <param name="MatchingTransactionIds">The IDs of transactions that matched this pattern.</param>
/// <param name="FirstOccurrence">The date of the earliest matching transaction.</param>
/// <param name="LastOccurrence">The date of the most recent matching transaction.</param>
/// <param name="MostUsedCategoryId">The most frequently used category ID from matching transactions, if any.</param>
public record DetectedPattern(
    string NormalizedDescription,
    string SampleDescription,
    MoneyValue AverageAmount,
    RecurrenceFrequency Frequency,
    int Interval,
    decimal Confidence,
    IReadOnlyList<Guid> MatchingTransactionIds,
    DateOnly FirstOccurrence,
    DateOnly LastOccurrence,
    Guid? MostUsedCategoryId);

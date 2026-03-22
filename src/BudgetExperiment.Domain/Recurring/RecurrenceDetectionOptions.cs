// <copyright file="RecurrenceDetectionOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Configuration options for the recurrence detection algorithm.
/// </summary>
/// <param name="MinimumOccurrences">Minimum number of matching transactions to consider a pattern (default 3).</param>
/// <param name="AmountVarianceTolerance">Allowed amount variance as a decimal fraction, e.g. 0.05 for ±5% (default 0.05).</param>
/// <param name="AnalysisWindowMonths">Number of months to analyze for patterns (default 12).</param>
/// <param name="MinimumConfidence">Minimum confidence score to include in results (default 0.5).</param>
public record RecurrenceDetectionOptions(
    int MinimumOccurrences = 3,
    decimal AmountVarianceTolerance = 0.05m,
    int AnalysisWindowMonths = 12,
    decimal MinimumConfidence = 0.5m);

// <copyright file="TransactionMatchResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents the result of matching a transaction to a recurring instance.
/// </summary>
/// <param name="RecurringTransactionId">The recurring transaction identifier.</param>
/// <param name="InstanceDate">The scheduled instance date.</param>
/// <param name="ConfidenceScore">The confidence score between 0 and 1.</param>
/// <param name="ConfidenceLevel">The confidence level (High, Medium, Low).</param>
/// <param name="AmountVariance">The variance between expected and actual amount (expected - actual).</param>
/// <param name="DateOffsetDays">The offset in days between actual and scheduled date (actual - scheduled).</param>
/// <param name="DescriptionSimilarity">The similarity score between descriptions (0 to 1).</param>
public sealed record TransactionMatchResult(
    Guid RecurringTransactionId,
    DateOnly InstanceDate,
    decimal ConfidenceScore,
    MatchConfidenceLevel ConfidenceLevel,
    decimal AmountVariance,
    int DateOffsetDays,
    decimal DescriptionSimilarity);

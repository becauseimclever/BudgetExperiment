// <copyright file="ReconciliationMatchDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a reconciliation match between an imported transaction and a recurring instance.
/// </summary>
public sealed record ReconciliationMatchDto
{
    /// <summary>
    /// Gets the match identifier.
    /// </summary>
    public Guid Id
    {
        get; init;
    }

    /// <summary>
    /// Gets the imported transaction identifier.
    /// </summary>
    public Guid ImportedTransactionId
    {
        get; init;
    }

    /// <summary>
    /// Gets the recurring transaction identifier.
    /// </summary>
    public Guid RecurringTransactionId
    {
        get; init;
    }

    /// <summary>
    /// Gets the recurring instance date.
    /// </summary>
    public DateOnly RecurringInstanceDate
    {
        get; init;
    }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal ConfidenceScore
    {
        get; init;
    }

    /// <summary>
    /// Gets the confidence level (High, Medium, Low).
    /// </summary>
    public string ConfidenceLevel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the match status (Suggested, Accepted, Rejected, AutoMatched).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the match source (Auto, Manual).
    /// </summary>
    public string Source { get; init; } = "Auto";

    /// <summary>
    /// Gets the variance between expected and actual amount.
    /// </summary>
    public decimal AmountVariance
    {
        get; init;
    }

    /// <summary>
    /// Gets the offset in days from scheduled date.
    /// </summary>
    public int DateOffsetDays
    {
        get; init;
    }

    /// <summary>
    /// Gets the UTC timestamp when the match was created.
    /// </summary>
    public DateTime CreatedAtUtc
    {
        get; init;
    }

    /// <summary>
    /// Gets the UTC timestamp when the match was resolved.
    /// </summary>
    public DateTime? ResolvedAtUtc
    {
        get; init;
    }

    /// <summary>
    /// Gets the imported transaction details (optional, populated when needed).
    /// </summary>
    public TransactionDto? ImportedTransaction
    {
        get; init;
    }

    /// <summary>
    /// Gets the recurring transaction description (optional, for display).
    /// </summary>
    public string? RecurringTransactionDescription
    {
        get; init;
    }

    /// <summary>
    /// Gets the expected amount from the recurring transaction.
    /// </summary>
    public MoneyDto? ExpectedAmount
    {
        get; init;
    }
}

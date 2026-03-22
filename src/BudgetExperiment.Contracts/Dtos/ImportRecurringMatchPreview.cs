// <copyright file="ImportRecurringMatchPreview.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Preview of a potential recurring transaction match during import.
/// </summary>
public sealed record ImportRecurringMatchPreview
{
    /// <summary>
    /// Gets the recurring transaction ID.
    /// </summary>
    public Guid RecurringTransactionId
    {
        get; init;
    }

    /// <summary>
    /// Gets the recurring transaction description.
    /// </summary>
    public string RecurringDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the instance date that would be matched.
    /// </summary>
    public DateOnly InstanceDate
    {
        get; init;
    }

    /// <summary>
    /// Gets the expected amount from the recurring transaction.
    /// </summary>
    public decimal ExpectedAmount
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
    /// Gets a value indicating whether this match would be auto-applied.
    /// </summary>
    public bool WouldAutoMatch
    {
        get; init;
    }
}

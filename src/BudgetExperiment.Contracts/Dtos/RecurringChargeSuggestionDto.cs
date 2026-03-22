// <copyright file="RecurringChargeSuggestionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a recurring charge suggestion.
/// </summary>
public sealed class RecurringChargeSuggestionDto
{
    /// <summary>
    /// Gets or sets the suggestion identifier.
    /// </summary>
    public Guid Id
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public Guid AccountId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the normalized description used for grouping.
    /// </summary>
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original sample description for display.
    /// </summary>
    public string SampleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the average amount.
    /// </summary>
    public MoneyDto AverageAmount { get; set; } = new();

    /// <summary>
    /// Gets or sets the detected recurrence frequency.
    /// </summary>
    public string DetectedFrequency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected interval between occurrences.
    /// </summary>
    public int DetectedInterval
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the confidence score (0.0–1.0).
    /// </summary>
    public decimal Confidence
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of matching transactions found.
    /// </summary>
    public int MatchingTransactionCount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the date of the first occurrence.
    /// </summary>
    public DateOnly FirstOccurrence
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the date of the last occurrence.
    /// </summary>
    public DateOnly LastOccurrence
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the most-used category identifier, if any.
    /// </summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the suggestion status (Pending, Accepted, Dismissed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accepted recurring transaction identifier, if accepted.
    /// </summary>
    public Guid? AcceptedRecurringTransactionId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAtUtc
    {
        get; set;
    }
}

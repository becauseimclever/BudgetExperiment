// <copyright file="RecurringTransactionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning recurring transaction details.
/// </summary>
public sealed class RecurringTransactionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences.</summary>
    public int Interval { get; set; }

    /// <summary>Gets or sets the day of month (1-31) for monthly patterns.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week for weekly patterns.</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear { get; set; }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the next occurrence date.</summary>
    public DateOnly NextOccurrence { get; set; }

    /// <summary>Gets or sets a value indicating whether the recurring transaction is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Gets or sets the concurrency version token for optimistic concurrency.</summary>
    public string? Version { get; set; }
}

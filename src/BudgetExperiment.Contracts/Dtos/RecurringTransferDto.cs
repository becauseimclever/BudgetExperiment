// <copyright file="RecurringTransferDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning recurring transfer details.
/// </summary>
public sealed class RecurringTransferDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount (always positive).</summary>
    public MoneyDto Amount { get; set; } = new();

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

    /// <summary>Gets or sets a value indicating whether the recurring transfer is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

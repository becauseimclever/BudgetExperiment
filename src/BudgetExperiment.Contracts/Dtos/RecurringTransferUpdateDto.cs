// <copyright file="RecurringTransferUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating a recurring transfer.
/// </summary>
public sealed class RecurringTransferUpdateDto
{
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount (must be positive).</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences.</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Gets or sets the day of month (1-31) for monthly patterns.</summary>
    public int? DayOfMonth
    {
        get; set;
    }

    /// <summary>Gets or sets the day of week for weekly patterns.</summary>
    public string? DayOfWeek
    {
        get; set;
    }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear
    {
        get; set;
    }

    /// <summary>Gets or sets the optional end date (null to remove).</summary>
    public DateOnly? EndDate
    {
        get; set;
    }
}

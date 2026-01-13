// <copyright file="CalendarDaySummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single day in the calendar grid with pre-computed totals.
/// </summary>
public sealed class CalendarDaySummaryDto
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets a value indicating whether this day is in the currently displayed month.</summary>
    public bool IsCurrentMonth { get; set; }

    /// <summary>Gets or sets a value indicating whether this day is today.</summary>
    public bool IsToday { get; set; }

    /// <summary>Gets or sets the total of actual transactions for this day.</summary>
    public MoneyDto ActualTotal { get; set; } = new();

    /// <summary>Gets or sets the total of projected recurring transactions for this day.</summary>
    public MoneyDto ProjectedTotal { get; set; } = new();

    /// <summary>Gets or sets the combined total (actual + projected) for this day.</summary>
    public MoneyDto CombinedTotal { get; set; } = new();

    /// <summary>Gets or sets the count of actual transactions.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the count of recurring transaction instances.</summary>
    public int RecurringCount { get; set; }

    /// <summary>Gets or sets a value indicating whether this day has recurring transactions.</summary>
    public bool HasRecurring { get; set; }

    /// <summary>Gets or sets the running balance at the end of this day.</summary>
    public MoneyDto EndOfDayBalance { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether the end-of-day balance is negative.</summary>
    public bool IsBalanceNegative { get; set; }
}

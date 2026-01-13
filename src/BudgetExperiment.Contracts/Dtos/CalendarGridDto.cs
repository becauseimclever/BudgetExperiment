// <copyright file="CalendarGridDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a complete calendar grid response with all data pre-computed.
/// </summary>
public sealed class CalendarGridDto
{
    /// <summary>Gets or sets the year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the days in the calendar grid (42 days for 6 weeks).</summary>
    public IReadOnlyList<CalendarDaySummaryDto> Days { get; set; } = [];

    /// <summary>Gets or sets the month summary with totals.</summary>
    public CalendarMonthSummaryDto MonthSummary { get; set; } = new();

    /// <summary>Gets or sets the starting balance at the beginning of the first day in the grid.</summary>
    public MoneyDto StartingBalance { get; set; } = new();
}

// <copyright file="CalendarMonthSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for monthly summary totals in the calendar.
/// </summary>
public sealed class CalendarMonthSummaryDto
{
    /// <summary>Gets or sets the total income for the month (actual transactions).</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the total expenses for the month (actual transactions).</summary>
    public MoneyDto TotalExpenses { get; set; } = new();

    /// <summary>Gets or sets the net change for the month (income - expenses).</summary>
    public MoneyDto NetChange { get; set; } = new();

    /// <summary>Gets or sets the projected income for the month (recurring).</summary>
    public MoneyDto ProjectedIncome { get; set; } = new();

    /// <summary>Gets or sets the projected expenses for the month (recurring).</summary>
    public MoneyDto ProjectedExpenses { get; set; } = new();
}

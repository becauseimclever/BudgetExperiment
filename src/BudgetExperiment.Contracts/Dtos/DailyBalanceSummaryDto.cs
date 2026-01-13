// <copyright file="DailyBalanceSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for daily balance summary showing starting and ending balance for a day.
/// </summary>
public sealed class DailyBalanceSummaryDto
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the balance at the start of this day (before any transactions).</summary>
    public MoneyDto StartingBalance { get; set; } = new();

    /// <summary>Gets or sets the balance at the end of this day (after all transactions).</summary>
    public MoneyDto EndingBalance { get; set; } = new();

    /// <summary>Gets or sets the net change for this day (sum of all transactions).</summary>
    public MoneyDto DayTotal { get; set; } = new();

    /// <summary>Gets or sets the number of transactions on this day.</summary>
    public int TransactionCount { get; set; }
}

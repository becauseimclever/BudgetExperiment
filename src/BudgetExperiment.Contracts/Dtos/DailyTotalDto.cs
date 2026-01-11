// <copyright file="DailyTotalDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for daily transaction totals in calendar summary.
/// </summary>
public sealed class DailyTotalDto
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the total amount for the day.</summary>
    public MoneyDto Total { get; set; } = new();

    /// <summary>Gets or sets the number of transactions on this day.</summary>
    public int TransactionCount { get; set; }
}

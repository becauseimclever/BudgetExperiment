// <copyright file="DaySummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for single-day spending summary with category breakdown.
/// </summary>
public sealed class DaySummaryDto
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the total spending for the day.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for the day.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the net amount (income - spending) for the day.</summary>
    public MoneyDto NetAmount { get; set; } = new();

    /// <summary>Gets or sets the number of transactions.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the top spending categories for the day (up to 3).</summary>
    public IReadOnlyList<DayTopCategoryDto> TopCategories { get; set; } = [];
}

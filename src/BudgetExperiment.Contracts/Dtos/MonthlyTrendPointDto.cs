// <copyright file="MonthlyTrendPointDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single month data point in the spending trends report.
/// </summary>
public sealed class MonthlyTrendPointDto
{
    /// <summary>Gets or sets the year.</summary>
    public int Year
    {
        get; set;
    }

    /// <summary>Gets or sets the month (1-12).</summary>
    public int Month
    {
        get; set;
    }

    /// <summary>Gets or sets the total spending for this month.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for this month.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the net amount (income - spending) for this month.</summary>
    public MoneyDto NetAmount { get; set; } = new();

    /// <summary>Gets or sets the number of transactions in this month.</summary>
    public int TransactionCount
    {
        get; set;
    }
}

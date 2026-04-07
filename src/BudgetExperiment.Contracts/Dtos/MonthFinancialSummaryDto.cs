// <copyright file="MonthFinancialSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Financial summary for a given month, including Kakeibo breakdown and the user's reflection.
/// </summary>
public sealed class MonthFinancialSummaryDto
{
    /// <summary>
    /// Gets or sets the total income for the month (sum of positive-amount transactions).
    /// </summary>
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Gets or sets the total expenses for the month (sum of absolute values of negative-amount transactions).
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Gets or sets the computed savings for the month (TotalIncome minus TotalExpenses).
    /// </summary>
    public decimal ComputedSavings { get; set; }

    /// <summary>
    /// Gets or sets the Kakeibo category breakdown of expenses.
    /// </summary>
    public KakeiboBreakdownDto ExpenseBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the user's reflection for this month, or null if not yet created.
    /// </summary>
    public MonthlyReflectionDto? Reflection { get; set; }
}

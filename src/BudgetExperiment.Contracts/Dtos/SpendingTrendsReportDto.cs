// <copyright file="SpendingTrendsReportDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for spending trends report over multiple months.
/// </summary>
public sealed class SpendingTrendsReportDto
{
    /// <summary>Gets or sets the monthly data points.</summary>
    public IReadOnlyList<MonthlyTrendPointDto> MonthlyData { get; set; } = [];

    /// <summary>Gets or sets the average monthly spending.</summary>
    public MoneyDto AverageMonthlySpending { get; set; } = new();

    /// <summary>Gets or sets the average monthly income.</summary>
    public MoneyDto AverageMonthlyIncome { get; set; } = new();

    /// <summary>Gets or sets the trend direction ("increasing", "stable", or "decreasing").</summary>
    public string TrendDirection { get; set; } = "stable";

    /// <summary>Gets or sets the trend percentage change.</summary>
    public decimal TrendPercentage
    {
        get; set;
    }
}

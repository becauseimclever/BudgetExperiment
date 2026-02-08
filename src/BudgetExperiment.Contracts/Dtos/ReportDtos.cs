// <copyright file="ReportDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for monthly category spending report.
/// </summary>
public sealed class MonthlyCategoryReportDto
{
    /// <summary>Gets or sets the year of the report.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month of the report (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the total spending for the month.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for the month.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the spending breakdown by category.</summary>
    public IReadOnlyList<CategorySpendingDto> Categories { get; set; } = [];
}

/// <summary>
/// DTO for category spending within a report.
/// </summary>
public sealed class CategorySpendingDto
{
    /// <summary>Gets or sets the category identifier (null for uncategorized).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the category color (hex code).</summary>
    public string? CategoryColor { get; set; }

    /// <summary>Gets or sets the total amount spent in this category.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the percentage of total spending this category represents.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Gets or sets the number of transactions in this category.</summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// DTO for category spending report over an arbitrary date range.
/// </summary>
public sealed class DateRangeCategoryReportDto
{
    /// <summary>Gets or sets the start date of the report range.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the end date of the report range.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Gets or sets the total spending for the range.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for the range.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the spending breakdown by category.</summary>
    public IReadOnlyList<CategorySpendingDto> Categories { get; set; } = [];
}

/// <summary>
/// DTO for a single month data point in the spending trends report.
/// </summary>
public sealed class MonthlyTrendPointDto
{
    /// <summary>Gets or sets the year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the total spending for this month.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for this month.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the net amount (income - spending) for this month.</summary>
    public MoneyDto NetAmount { get; set; } = new();

    /// <summary>Gets or sets the number of transactions in this month.</summary>
    public int TransactionCount { get; set; }
}

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
    public decimal TrendPercentage { get; set; }
}

/// <summary>
/// DTO for a top category in a day summary.
/// </summary>
public sealed class DayTopCategoryDto
{
    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount spent in this category.</summary>
    public MoneyDto Amount { get; set; } = new();
}

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

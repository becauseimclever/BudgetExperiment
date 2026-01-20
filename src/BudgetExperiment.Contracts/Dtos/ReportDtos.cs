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

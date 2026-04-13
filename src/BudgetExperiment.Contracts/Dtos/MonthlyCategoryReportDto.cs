// <copyright file="MonthlyCategoryReportDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for monthly category spending report.
/// </summary>
public sealed class MonthlyCategoryReportDto
{
    /// <summary>Gets or sets the year of the report.</summary>
    public int Year
    {
        get; set;
    }

    /// <summary>Gets or sets the month of the report (1-12).</summary>
    public int Month
    {
        get; set;
    }

    /// <summary>Gets or sets the total spending for the month.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for the month.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the spending breakdown by category.</summary>
    public IReadOnlyList<CategorySpendingDto> Categories { get; set; } = [];

    /// <summary>Gets or sets the optional Kakeibo grouped summary.</summary>
    public KakeiboGroupedSummaryDto? KakeiboGroupedSummary
    {
        get; set;
    }
}

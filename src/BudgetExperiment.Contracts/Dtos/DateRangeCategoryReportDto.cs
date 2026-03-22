// <copyright file="DateRangeCategoryReportDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for category spending report over an arbitrary date range.
/// </summary>
public sealed class DateRangeCategoryReportDto
{
    /// <summary>Gets or sets the start date of the report range.</summary>
    public DateOnly StartDate
    {
        get; set;
    }

    /// <summary>Gets or sets the end date of the report range.</summary>
    public DateOnly EndDate
    {
        get; set;
    }

    /// <summary>Gets or sets the total spending for the range.</summary>
    public MoneyDto TotalSpending { get; set; } = new();

    /// <summary>Gets or sets the total income for the range.</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the spending breakdown by category.</summary>
    public IReadOnlyList<CategorySpendingDto> Categories { get; set; } = [];
}

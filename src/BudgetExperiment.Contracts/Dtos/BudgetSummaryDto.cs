// <copyright file="BudgetSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for overall budget summary.
/// </summary>
public sealed class BudgetSummaryDto
{
    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int Year
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public int Month
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total budgeted amount.
    /// </summary>
    public MoneyDto TotalBudgeted { get; set; } = new();

    /// <summary>
    /// Gets or sets the total spent amount.
    /// </summary>
    public MoneyDto TotalSpent { get; set; } = new();

    /// <summary>
    /// Gets or sets the total remaining amount.
    /// </summary>
    public MoneyDto TotalRemaining { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall percentage used.
    /// </summary>
    public decimal OverallPercentUsed
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of categories on track.
    /// </summary>
    public int CategoriesOnTrack
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of categories with warning status.
    /// </summary>
    public int CategoriesWarning
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of categories over budget.
    /// </summary>
    public int CategoriesOverBudget
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of categories with no budget set.
    /// </summary>
    public int CategoriesNoBudgetSet
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the progress for each category.
    /// </summary>
    public IReadOnlyList<BudgetProgressDto> CategoryProgress { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional Kakeibo grouped summary.
    /// </summary>
    public KakeiboGroupedSummaryDto? KakeiboGroupedSummary
    {
        get; set;
    }
}

// <copyright file="BudgetProgressDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for budget progress.
/// </summary>
public sealed class BudgetProgressDto
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category icon.
    /// </summary>
    public string? CategoryIcon { get; set; }

    /// <summary>
    /// Gets or sets the category color.
    /// </summary>
    public string? CategoryColor { get; set; }

    /// <summary>
    /// Gets or sets the target amount.
    /// </summary>
    public MoneyDto TargetAmount { get; set; } = new();

    /// <summary>
    /// Gets or sets the amount spent.
    /// </summary>
    public MoneyDto SpentAmount { get; set; } = new();

    /// <summary>
    /// Gets or sets the remaining amount.
    /// </summary>
    public MoneyDto RemainingAmount { get; set; } = new();

    /// <summary>
    /// Gets or sets the percentage of budget used.
    /// </summary>
    public decimal PercentUsed { get; set; }

    /// <summary>
    /// Gets or sets the budget status (OnTrack, Warning, OverBudget, NoBudgetSet).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// Data transfer object for overall budget summary.
/// </summary>
public sealed class BudgetSummaryDto
{
    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public int Month { get; set; }

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
    public decimal OverallPercentUsed { get; set; }

    /// <summary>
    /// Gets or sets the number of categories on track.
    /// </summary>
    public int CategoriesOnTrack { get; set; }

    /// <summary>
    /// Gets or sets the number of categories with warning status.
    /// </summary>
    public int CategoriesWarning { get; set; }

    /// <summary>
    /// Gets or sets the number of categories over budget.
    /// </summary>
    public int CategoriesOverBudget { get; set; }

    /// <summary>
    /// Gets or sets the progress for each category.
    /// </summary>
    public IReadOnlyList<BudgetProgressDto> CategoryProgress { get; set; } = [];
}

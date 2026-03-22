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
    public Guid CategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category icon.
    /// </summary>
    public string? CategoryIcon
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category color.
    /// </summary>
    public string? CategoryColor
    {
        get; set;
    }

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
    public decimal PercentUsed
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the budget status (OnTrack, Warning, OverBudget, NoBudgetSet).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount
    {
        get; set;
    }
}

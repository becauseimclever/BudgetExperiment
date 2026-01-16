// <copyright file="BudgetGoalDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for a budget goal.
/// </summary>
public sealed class BudgetGoalDto
{
    /// <summary>
    /// Gets or sets the goal identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the target amount.
    /// </summary>
    public MoneyDto TargetAmount { get; set; } = new();
}

/// <summary>
/// Data transfer object for setting a budget goal.
/// </summary>
public sealed class BudgetGoalSetDto
{
    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the target amount.
    /// </summary>
    public MoneyDto TargetAmount { get; set; } = new();
}

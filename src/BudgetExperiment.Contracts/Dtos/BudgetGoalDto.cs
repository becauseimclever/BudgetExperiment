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

/// <summary>
/// Request DTO for copying budget goals from one month to another.
/// </summary>
public sealed class CopyBudgetGoalsRequest
{
    /// <summary>
    /// Gets or sets the source year to copy goals from.
    /// </summary>
    public int SourceYear { get; set; }

    /// <summary>
    /// Gets or sets the source month to copy goals from (1-12).
    /// </summary>
    public int SourceMonth { get; set; }

    /// <summary>
    /// Gets or sets the target year to copy goals to.
    /// </summary>
    public int TargetYear { get; set; }

    /// <summary>
    /// Gets or sets the target month to copy goals to (1-12).
    /// </summary>
    public int TargetMonth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing goals in the target month.
    /// When false, only goals for categories without existing goals will be created.
    /// </summary>
    public bool OverwriteExisting { get; set; }
}

/// <summary>
/// Response DTO for the copy budget goals operation.
/// </summary>
public sealed class CopyBudgetGoalsResult
{
    /// <summary>
    /// Gets or sets the number of goals that were created.
    /// </summary>
    public int GoalsCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of goals that were updated (when overwrite was enabled).
    /// </summary>
    public int GoalsUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of goals that were skipped (already existed and overwrite was disabled).
    /// </summary>
    public int GoalsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the total goals in the source month.
    /// </summary>
    public int SourceGoalsCount { get; set; }
}

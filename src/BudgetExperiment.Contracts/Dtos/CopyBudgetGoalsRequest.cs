// <copyright file="CopyBudgetGoalsRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for copying budget goals from one month to another.
/// </summary>
public sealed class CopyBudgetGoalsRequest
{
    /// <summary>
    /// Gets or sets the source year to copy goals from.
    /// </summary>
    public int SourceYear
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the source month to copy goals from (1-12).
    /// </summary>
    public int SourceMonth
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the target year to copy goals to.
    /// </summary>
    public int TargetYear
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the target month to copy goals to (1-12).
    /// </summary>
    public int TargetMonth
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing goals in the target month.
    /// When false, only goals for categories without existing goals will be created.
    /// </summary>
    public bool OverwriteExisting
    {
        get; set;
    }
}

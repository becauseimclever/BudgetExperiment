// <copyright file="CopyBudgetGoalsResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO for the copy budget goals operation.
/// </summary>
public sealed class CopyBudgetGoalsResult
{
    /// <summary>
    /// Gets or sets the number of goals that were created.
    /// </summary>
    public int GoalsCreated
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of goals that were updated (when overwrite was enabled).
    /// </summary>
    public int GoalsUpdated
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of goals that were skipped (already existed and overwrite was disabled).
    /// </summary>
    public int GoalsSkipped
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total goals in the source month.
    /// </summary>
    public int SourceGoalsCount
    {
        get; set;
    }
}

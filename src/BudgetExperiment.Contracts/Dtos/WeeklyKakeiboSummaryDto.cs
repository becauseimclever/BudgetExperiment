// <copyright file="WeeklyKakeiboSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Spending aggregation for a single ISO week broken down by Kakeibo category,
/// with an optional Kaizen micro-goal outcome overlay.
/// </summary>
public sealed class WeeklyKakeiboSummaryDto
{
    /// <summary>
    /// Gets or sets the Monday of the ISO week this row represents.
    /// </summary>
    public DateOnly WeekStart
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the human-readable week label, e.g. "Apr 7–13".
    /// </summary>
    public string WeekLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total spending in the Essentials (必要) bucket for the week.
    /// </summary>
    public decimal Essentials
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spending in the Wants (欲しい) bucket for the week.
    /// </summary>
    public decimal Wants
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spending in the Culture (文化) bucket for the week.
    /// </summary>
    public decimal Culture
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spending in the Unexpected (予期しない) bucket for the week.
    /// </summary>
    public decimal Unexpected
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the Kaizen micro-goal description for the week, or <c>null</c> if no goal was set.
    /// </summary>
    public string? KaizenGoalDescription
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets whether the Kaizen micro-goal was achieved, or <c>null</c> if no goal was set.
    /// </summary>
    public bool? KaizenGoalAchieved
    {
        get; set;
    }
}

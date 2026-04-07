// <copyright file="KaizenGoalDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO representing a Kaizen micro-goal.
/// </summary>
public sealed class KaizenGoalDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Monday of the ISO week this goal applies to.
    /// </summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary>
    /// Gets or sets the goal description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional numeric target amount.
    /// </summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>
    /// Gets or sets the optional Kakeibo category scope as a string (for API serialization).
    /// </summary>
    public string? KakeiboCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the goal was achieved.
    /// </summary>
    public bool IsAchieved { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the goal was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the goal was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}

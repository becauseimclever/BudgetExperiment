// <copyright file="UpdateKaizenGoalDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for updating an existing Kaizen micro-goal.
/// </summary>
public sealed class UpdateKaizenGoalDto
{
    /// <summary>
    /// Gets or sets the updated goal description (required, max 500 characters).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated optional target amount (must be non-negative if provided).
    /// </summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>
    /// Gets or sets the updated optional Kakeibo category scope as a string.
    /// Accepted values: Essentials, Wants, Culture, Unexpected.
    /// </summary>
    public string? KakeiboCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the goal was achieved.
    /// </summary>
    public bool IsAchieved { get; set; }
}

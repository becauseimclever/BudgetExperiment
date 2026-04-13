// <copyright file="CreateKaizenGoalDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for creating a new Kaizen micro-goal.
/// </summary>
public sealed class CreateKaizenGoalDto
{
    /// <summary>
    /// Gets or sets the goal description (required, max 500 characters).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional numeric target amount (must be non-negative if provided).
    /// </summary>
    public decimal? TargetAmount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional Kakeibo category scope as a string.
    /// Accepted values: Essentials, Wants, Culture, Unexpected.
    /// </summary>
    public string? KakeiboCategory
    {
        get; set;
    }
}

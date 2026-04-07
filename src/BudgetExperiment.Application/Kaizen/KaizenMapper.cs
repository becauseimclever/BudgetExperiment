// <copyright file="KaizenMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Kaizen;

namespace BudgetExperiment.Application.Kaizen;

/// <summary>
/// Static mapper between <see cref="KaizenGoal"/> domain entities and <see cref="KaizenGoalDto"/> response DTOs.
/// </summary>
internal static class KaizenMapper
{
    /// <summary>
    /// Maps a <see cref="KaizenGoal"/> to a <see cref="KaizenGoalDto"/>.
    /// </summary>
    /// <param name="goal">The domain entity.</param>
    /// <returns>The mapped DTO.</returns>
    internal static KaizenGoalDto ToDto(KaizenGoal goal)
    {
        return new KaizenGoalDto
        {
            Id = goal.Id,
            WeekStartDate = goal.WeekStartDate,
            Description = goal.Description,
            TargetAmount = goal.TargetAmount,
            KakeiboCategory = goal.KakeiboCategory?.ToString(),
            IsAchieved = goal.IsAchieved,
            CreatedAtUtc = goal.CreatedAtUtc,
            UpdatedAtUtc = goal.UpdatedAtUtc,
        };
    }
}

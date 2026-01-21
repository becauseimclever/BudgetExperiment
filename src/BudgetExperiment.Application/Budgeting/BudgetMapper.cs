// <copyright file="BudgetMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Mappers for budget-related domain entities to DTOs.
/// </summary>
public static class BudgetMapper
{
    /// <summary>
    /// Maps a <see cref="BudgetCategory"/> to a <see cref="BudgetCategoryDto"/>.
    /// </summary>
    /// <param name="category">The budget category entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetCategoryDto ToDto(BudgetCategory category)
    {
        return new BudgetCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            Icon = category.Icon,
            Color = category.Color,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
        };
    }

    /// <summary>
    /// Maps a <see cref="BudgetGoal"/> to a <see cref="BudgetGoalDto"/>.
    /// </summary>
    /// <param name="goal">The budget goal entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetGoalDto ToDto(BudgetGoal goal)
    {
        return new BudgetGoalDto
        {
            Id = goal.Id,
            CategoryId = goal.CategoryId,
            Year = goal.Year,
            Month = goal.Month,
            TargetAmount = CommonMapper.ToDto(goal.TargetAmount),
        };
    }

    /// <summary>
    /// Maps a <see cref="BudgetProgress"/> to a <see cref="BudgetProgressDto"/>.
    /// </summary>
    /// <param name="progress">The budget progress value object.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetProgressDto ToDto(BudgetProgress progress)
    {
        return new BudgetProgressDto
        {
            CategoryId = progress.CategoryId,
            CategoryName = progress.CategoryName,
            CategoryIcon = progress.CategoryIcon,
            CategoryColor = progress.CategoryColor,
            TargetAmount = CommonMapper.ToDto(progress.TargetAmount),
            SpentAmount = CommonMapper.ToDto(progress.SpentAmount),
            RemainingAmount = CommonMapper.ToDto(progress.RemainingAmount),
            PercentUsed = progress.PercentUsed,
            Status = progress.Status.ToString(),
            TransactionCount = progress.TransactionCount,
        };
    }
}

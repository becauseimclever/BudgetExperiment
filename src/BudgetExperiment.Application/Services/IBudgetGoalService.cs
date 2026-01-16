// <copyright file="IBudgetGoalService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for budget goal operations.
/// </summary>
public interface IBudgetGoalService
{
    /// <summary>
    /// Gets a budget goal by its identifier.
    /// </summary>
    /// <param name="id">The goal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The goal DTO, or null if not found.</returns>
    Task<BudgetGoalDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budget goals for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of goal DTOs for the month.</returns>
    Task<IReadOnlyList<BudgetGoalDto>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budget goals for a specific category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of goal DTOs for the category.</returns>
    Task<IReadOnlyList<BudgetGoalDto>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a budget goal for a category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="dto">The goal data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated goal DTO, or null if category not found.</returns>
    Task<BudgetGoalDto?> SetGoalAsync(Guid categoryId, BudgetGoalSetDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget goal for a category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully deleted, false if not found.</returns>
    Task<bool> DeleteGoalAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default);
}

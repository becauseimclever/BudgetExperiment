// <copyright file="IKaizenGoalService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Kaizen;

/// <summary>
/// Service interface for Kaizen micro-goal operations.
/// </summary>
public interface IKaizenGoalService
{
    /// <summary>
    /// Gets the goal for the specified user and week, or null if none exists.
    /// </summary>
    /// <param name="weekStart">The Monday of the ISO week.</param>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The goal DTO, or null.</returns>
    Task<KaizenGoalDto?> GetByWeekAsync(DateOnly weekStart, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new goal for the specified week.
    /// </summary>
    /// <param name="weekStart">The Monday of the ISO week.</param>
    /// <param name="dto">The creation data.</param>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created goal DTO.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a goal already exists for the week.</exception>
    Task<KaizenGoalDto> CreateAsync(DateOnly weekStart, CreateKaizenGoalDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing goal.
    /// </summary>
    /// <param name="goalId">The goal identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated goal DTO, or null if not found.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    Task<KaizenGoalDto?> UpdateAsync(Guid goalId, UpdateKaizenGoalDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all goals for a user within an inclusive week range.
    /// </summary>
    /// <param name="fromWeek">The earliest week start date (inclusive).</param>
    /// <param name="toWeek">The latest week start date (inclusive).</param>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of goal DTOs ordered by week descending.</returns>
    Task<IReadOnlyList<KaizenGoalDto>> GetRangeAsync(DateOnly fromWeek, DateOnly toWeek, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a goal owned by the requesting user.
    /// </summary>
    /// <param name="goalId">The goal identifier.</param>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the goal was deleted; false if not found or not owned by user.</returns>
    Task<bool> DeleteAsync(Guid goalId, Guid userId, CancellationToken ct = default);
}

// <copyright file="IKaizenGoalRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="KaizenGoal"/> aggregate.
/// </summary>
public interface IKaizenGoalRepository
{
    /// <summary>
    /// Gets the goal for a specific user and week start date.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="weekStart">The Monday of the ISO week.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The goal, or null if none exists for that week.</returns>
    Task<KaizenGoal?> GetByUserWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default);

    /// <summary>
    /// Gets a goal by its unique identifier.
    /// </summary>
    /// <param name="id">The goal identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The goal, or null if not found.</returns>
    Task<KaizenGoal?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all goals for a user within an inclusive week range ordered by week descending.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="fromWeek">The earliest week start date (inclusive).</param>
    /// <param name="toWeek">The latest week start date (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of goals ordered by <see cref="KaizenGoal.WeekStartDate"/> descending.</returns>
    Task<IReadOnlyList<KaizenGoal>> GetRangeAsync(Guid userId, DateOnly fromWeek, DateOnly toWeek, CancellationToken ct = default);

    /// <summary>
    /// Adds a new goal to the context (pending save).
    /// </summary>
    /// <param name="goal">The goal to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddAsync(KaizenGoal goal, CancellationToken ct = default);

    /// <summary>
    /// Removes a goal from the context (pending save).
    /// </summary>
    /// <param name="goal">The goal to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveAsync(KaizenGoal goal, CancellationToken ct = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveChangesAsync(CancellationToken ct = default);
}

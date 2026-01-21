// <copyright file="IBudgetGoalRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for budget goals.
/// </summary>
public interface IBudgetGoalRepository : IReadRepository<BudgetGoal>, IWriteRepository<BudgetGoal>
{
    /// <summary>
    /// Gets a budget goal for a specific category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget goal or null if not found.</returns>
    Task<BudgetGoal?> GetByCategoryAndMonthAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budget goals for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of budget goals for the month.</returns>
    Task<IReadOnlyList<BudgetGoal>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budget goals for a specific category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of budget goals for the category.</returns>
    Task<IReadOnlyList<BudgetGoal>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

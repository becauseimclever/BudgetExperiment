// <copyright file="IMonthlyReflectionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="MonthlyReflection"/> aggregate.
/// </summary>
public interface IMonthlyReflectionRepository
{
    /// <summary>
    /// Gets a reflection by user and month, or null if none exists.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reflection, or null if not found.</returns>
    Task<MonthlyReflection?> GetByUserMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated history of reflections for a user in reverse chronological order.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="offset">Number of results to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of reflections.</returns>
    Task<IReadOnlyList<MonthlyReflection>> GetHistoryAsync(
        Guid userId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of reflections for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a reflection by its unique identifier.
    /// </summary>
    /// <param name="id">The reflection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reflection, or null if not found.</returns>
    Task<MonthlyReflection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new reflection to the store.
    /// </summary>
    /// <param name="reflection">The reflection to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddAsync(MonthlyReflection reflection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a reflection from the store.
    /// </summary>
    /// <param name="reflection">The reflection to remove.</param>
    void Remove(MonthlyReflection reflection);

    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// <copyright file="IBudgetCategoryRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for budget categories.
/// </summary>
public interface IBudgetCategoryRepository : IReadRepository<BudgetCategory>, IWriteRepository<BudgetCategory>
{
    /// <summary>
    /// Gets a category by name.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category or null if not found.</returns>
    Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active categories ordered by sort order then name.</returns>
    Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories of a specific type.
    /// </summary>
    /// <param name="type">The category type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of categories of the specified type.</returns>
    Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories including inactive ones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all categories ordered by sort order then name.</returns>
    Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default);
}

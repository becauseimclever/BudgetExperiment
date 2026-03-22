// <copyright file="ICategorizationRuleRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for categorization rules.
/// </summary>
public interface ICategorizationRuleRepository : IReadRepository<CategorizationRule>, IWriteRepository<CategorizationRule>
{
    /// <summary>
    /// Gets all active rules ordered by priority (ascending - lower number = higher priority).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active rules in priority order.</returns>
    Task<IReadOnlyList<CategorizationRule>> GetActiveByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rules for a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rules associated with the category.</returns>
    Task<IReadOnlyList<CategorizationRule>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available priority value (max current priority + 1, or 1 if no rules exist).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next priority value to use.</returns>
    Task<int> GetNextPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates rule priorities.
    /// </summary>
    /// <param name="priorities">Collection of rule ID and new priority pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReorderPrioritiesAsync(IEnumerable<(Guid RuleId, int NewPriority)> priorities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists rules with server-side pagination, filtering, and sorting.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="search">Optional search text to filter by name or pattern.</param>
    /// <param name="categoryId">Optional category ID filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="sortBy">Optional sort field (priority, name, category, createdAt).</param>
    /// <param name="sortDirection">Optional sort direction (asc or desc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of matching items and total count.</returns>
    Task<(IReadOnlyList<CategorizationRule> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple rules by their IDs.
    /// </summary>
    /// <param name="ids">The rule identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching rules.</returns>
    Task<IReadOnlyList<CategorizationRule>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple rules.
    /// </summary>
    /// <param name="rules">The rules to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveBulkAsync(IReadOnlyList<CategorizationRule> rules);
}

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
}

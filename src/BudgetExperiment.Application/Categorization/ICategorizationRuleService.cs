// <copyright file="ICategorizationRuleService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service interface for categorization rule operations.
/// </summary>
public interface ICategorizationRuleService
{
    /// <summary>
    /// Gets all categorization rules ordered by priority.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of categorization rules.</returns>
    Task<IReadOnlyList<CategorizationRuleDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a categorization rule by ID.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rule if found, null otherwise.</returns>
    Task<CategorizationRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new categorization rule.
    /// </summary>
    /// <param name="dto">The rule creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created rule.</returns>
    Task<CategorizationRuleDto> CreateAsync(CategorizationRuleCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple categorization rules from a category suggestion.
    /// </summary>
    /// <param name="categoryId">The category to assign the rules to.</param>
    /// <param name="patterns">The patterns to create rules for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created rules.</returns>
    Task<IReadOnlyList<CategorizationRuleDto>> CreateBulkFromPatternsAsync(
        Guid categoryId,
        IEnumerable<string> patterns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for conflicts between a pattern and existing rules.
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <param name="matchType">The match type.</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude from conflict check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conflicting rules.</returns>
    Task<IReadOnlyList<CategorizationRuleDto>> CheckConflictsAsync(
        string pattern,
        string matchType,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="dto">The rule update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated rule if found, null otherwise.</returns>
    Task<CategorizationRuleDto?> UpdateAsync(Guid id, CategorizationRuleUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a deactivated rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activated, false if not found.</returns>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an active rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated, false if not found.</returns>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders rules by setting new priorities.
    /// </summary>
    /// <param name="ruleIds">The ordered list of rule IDs. Index becomes the new priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task ReorderAsync(IReadOnlyList<Guid> ruleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a pattern against existing transaction descriptions.
    /// </summary>
    /// <param name="request">The test pattern request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result with matching descriptions.</returns>
    Task<TestPatternResponse> TestPatternAsync(TestPatternRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies categorization rules to transactions.
    /// </summary>
    /// <param name="request">The apply rules request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the bulk operation.</returns>
    Task<ApplyRulesResponse> ApplyRulesAsync(ApplyRulesRequest request, CancellationToken cancellationToken = default);
}

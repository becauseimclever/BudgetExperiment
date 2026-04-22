// <copyright file="IMerchantMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for resolving merchant-to-category mappings.
/// Combines default knowledge base with user-learned mappings.
/// </summary>
public interface IMerchantMappingService
{
    /// <summary>
    /// Gets a category mapping for a transaction description.
    /// Learned mappings take precedence over default mappings.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping result or null if no mapping found.</returns>
    Task<MerchantMappingResult?> GetMappingAsync(string ownerId, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all matching merchant patterns from a collection of transaction descriptions.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="descriptions">The transaction descriptions to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pattern matches grouped by category.</returns>
    Task<IReadOnlyList<PatternMatch>> FindMatchingPatternsAsync(
        string ownerId,
        IReadOnlyList<string> descriptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Learns a merchant-to-category mapping from a manual categorization.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="categoryId">The category ID the user assigned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LearnFromCategorizationAsync(
        string ownerId,
        string description,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all learned mappings for a user.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The learned mappings.</returns>
    Task<IReadOnlyList<LearnedMerchantMappingInfo>> GetLearnedMappingsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a learned mapping.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="mappingId">The mapping ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteLearnedMappingAsync(
        string ownerId,
        Guid mappingId,
        CancellationToken cancellationToken = default);
}

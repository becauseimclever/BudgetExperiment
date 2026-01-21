// <copyright file="ILearnedMerchantMappingRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for learned merchant mappings.
/// </summary>
public interface ILearnedMerchantMappingRepository : IReadRepository<LearnedMerchantMapping>, IWriteRepository<LearnedMerchantMapping>
{
    /// <summary>
    /// Gets a mapping by pattern for a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="pattern">The normalized merchant pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping or null if not found.</returns>
    Task<LearnedMerchantMapping?> GetByPatternAsync(string ownerId, string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings for a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All mappings for the user.</returns>
    Task<IReadOnlyList<LearnedMerchantMapping>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets mappings for a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mappings for the category.</returns>
    Task<IReadOnlyList<LearnedMerchantMapping>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a mapping exists for a pattern and user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="pattern">The normalized merchant pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the mapping exists.</returns>
    Task<bool> ExistsAsync(string ownerId, string pattern, CancellationToken cancellationToken = default);
}

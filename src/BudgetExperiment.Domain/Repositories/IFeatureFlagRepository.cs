// <copyright file="IFeatureFlagRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.FeatureFlags;

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository for feature flag persistence.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all feature flags ordered by name.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single feature flag by name.
    /// </summary>
    /// <param name="name">The flag name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag, or <c>null</c> if not found.</returns>
    Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a feature flag's enabled state.
    /// </summary>
    /// <param name="name">The flag name.</param>
    /// <param name="isEnabled">Whether the feature should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(string name, bool isEnabled, CancellationToken cancellationToken = default);
}

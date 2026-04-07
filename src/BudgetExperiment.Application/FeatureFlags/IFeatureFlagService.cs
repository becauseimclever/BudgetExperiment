// <copyright file="IFeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.FeatureFlags;

/// <summary>
/// Service for accessing and managing feature flags.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="flagName">The feature flag name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the flag exists and is enabled; otherwise <c>false</c>.</returns>
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags as a name-to-enabled dictionary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping flag names to their enabled state.</returns>
    Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a feature flag's enabled state (admin operation).
    /// </summary>
    /// <param name="flagName">The feature flag name.</param>
    /// <param name="isEnabled">Whether the feature should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
}

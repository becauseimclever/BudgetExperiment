// <copyright file="IFeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side service for accessing feature flags fetched from the API.
/// </summary>
public interface IFeatureFlagClientService
{
    /// <summary>
    /// Gets the current dictionary of feature flags (name → enabled).
    /// </summary>
    Dictionary<string, bool> Flags { get; }

    /// <summary>
    /// Returns whether the named feature flag is enabled.
    /// </summary>
    /// <param name="flagName">The feature flag name.</param>
    /// <returns><c>true</c> if the flag exists and is enabled; otherwise <c>false</c>.</returns>
    bool IsEnabled(string flagName);

    /// <summary>
    /// Loads feature flags from the API and caches them locally.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadFlagsAsync();

    /// <summary>
    /// Forces a refresh of feature flags from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RefreshAsync();
}

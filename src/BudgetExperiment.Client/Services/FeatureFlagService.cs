// <copyright file="FeatureFlagService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using System.Net.Http.Json;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client service for checking feature flags.
/// </summary>
public sealed class FeatureFlagService
{
    private readonly HttpClient _httpClient;
    private FeatureFlags? _cachedFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls.</param>
    public FeatureFlagService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the feature flags from the API (with caching).
    /// </summary>
    /// <returns>Feature flags.</returns>
    public async Task<FeatureFlags> GetFeatureFlagsAsync()
    {
        if (_cachedFlags is not null)
        {
            return _cachedFlags;
        }

        var response = await _httpClient.GetFromJsonAsync<FeatureFlags>("api/v1/featureflags");
        _cachedFlags = response ?? new FeatureFlags();
        return _cachedFlags;
    }
}

/// <summary>
/// Feature flags DTO.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>
    /// Gets or sets a value indicating whether the Quick Entry feature is enabled.
    /// </summary>
    public bool QuickEntry { get; set; }
}

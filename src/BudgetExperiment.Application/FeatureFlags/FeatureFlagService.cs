// <copyright file="FeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.FeatureFlags;
using BudgetExperiment.Domain.Repositories;

using Microsoft.Extensions.Caching.Memory;

namespace BudgetExperiment.Application.FeatureFlags;

/// <summary>
/// Service for accessing and managing feature flags with in-memory caching.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private const string CacheKey = "feature:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IFeatureFlagRepository _repository;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="repository">The feature flag repository.</param>
    /// <param name="cache">The in-memory cache.</param>
    public FeatureFlagService(IFeatureFlagRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default)
    {
        var flags = await this.GetAllAsync(cancellationToken);
        return flags.TryGetValue(flagName, out var isEnabled) && isEnabled;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<Dictionary<string, bool>>(CacheKey, out var cached))
        {
            return cached!;
        }

        var flags = await _repository.GetAllAsync(cancellationToken);
        var dict = flags.ToDictionary(f => f.Name, f => f.IsEnabled);
        _cache.Set(CacheKey, dict, CacheDuration);
        return dict;
    }

    /// <inheritdoc />
    public async Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(flagName, isEnabled, cancellationToken);
        _cache.Remove(CacheKey);
    }
}

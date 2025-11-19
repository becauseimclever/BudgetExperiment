// <copyright file="FeatureFlagService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using Microsoft.Extensions.Options;

namespace BudgetExperiment.Application.FeatureFlags;

/// <summary>
/// Service for checking feature flag status.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlags featureFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="featureFlags">Feature flag options.</param>
    public FeatureFlagService(IOptions<FeatureFlags> featureFlags)
    {
        this.featureFlags = featureFlags.Value;
    }

    /// <inheritdoc/>
    public bool IsQuickEntryEnabled()
    {
        return this.featureFlags.QuickEntry;
    }
}

// <copyright file="FeatureFlag.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.FeatureFlags;

/// <summary>
/// Feature flag entity. Controls gradual rollout of features to clients.
/// </summary>
public sealed class FeatureFlag
{
    /// <summary>
    /// Gets or sets the flag name (e.g., "Calendar:SpendingHeatmap").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

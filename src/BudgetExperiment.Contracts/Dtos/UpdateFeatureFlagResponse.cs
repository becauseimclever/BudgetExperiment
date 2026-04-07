// <copyright file="UpdateFeatureFlagResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response returned after updating a feature flag.
/// </summary>
public sealed class UpdateFeatureFlagResponse
{
    /// <summary>
    /// Gets or sets the feature flag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the feature flag is now enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the update.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}

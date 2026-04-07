// <copyright file="UpdateFeatureFlagRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to update a feature flag's enabled state.
/// </summary>
public sealed class UpdateFeatureFlagRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the feature flag should be enabled.
    /// </summary>
    public bool Enabled { get; set; }
}

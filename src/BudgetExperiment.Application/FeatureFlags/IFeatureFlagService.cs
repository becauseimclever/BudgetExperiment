// <copyright file="IFeatureFlagService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

namespace BudgetExperiment.Application.FeatureFlags;

/// <summary>
/// Service for checking feature flag status.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if the Quick Entry feature is enabled.
    /// </summary>
    /// <returns>True if enabled, false otherwise.</returns>
    bool IsQuickEntryEnabled();
}

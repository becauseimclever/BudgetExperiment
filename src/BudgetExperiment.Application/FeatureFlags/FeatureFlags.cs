// <copyright file="FeatureFlags.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

namespace BudgetExperiment.Application.FeatureFlags;

/// <summary>
/// Feature flag configuration options.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Gets or sets a value indicating whether the Quick Entry feature is enabled.
    /// </summary>
    public bool QuickEntry { get; set; }
}

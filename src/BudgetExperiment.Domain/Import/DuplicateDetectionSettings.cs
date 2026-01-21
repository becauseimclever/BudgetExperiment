// <copyright file="DuplicateDetectionSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Settings for detecting duplicate transactions during import.
/// </summary>
public sealed record DuplicateDetectionSettings
{
    /// <summary>
    /// Gets a value indicating whether duplicate detection is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the number of days to look back for duplicates.
    /// Default is 30 days.
    /// </summary>
    public int LookbackDays { get; init; } = 30;

    /// <summary>
    /// Gets the mode for matching transaction descriptions.
    /// Default is exact matching.
    /// </summary>
    public DescriptionMatchMode DescriptionMatch { get; init; } = DescriptionMatchMode.Exact;
}

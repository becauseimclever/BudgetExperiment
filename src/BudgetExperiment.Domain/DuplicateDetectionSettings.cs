// <copyright file="DuplicateDetectionSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Settings for detecting duplicate transactions during import.
/// </summary>
public sealed record DuplicateDetectionSettings
{
    /// <summary>
    /// Gets the number of days before and after to search for duplicates.
    /// Default is 1 (±1 day from transaction date).
    /// </summary>
    public int DateWindowDays { get; init; } = 1;

    /// <summary>
    /// Gets the percentage tolerance for amount matching.
    /// 0 means exact match, 1 means ±1% tolerance.
    /// Default is 0 (exact match).
    /// </summary>
    public decimal AmountTolerancePercent { get; init; } = 0m;

    /// <summary>
    /// Gets the mode for matching transaction descriptions.
    /// Default is fuzzy matching.
    /// </summary>
    public DescriptionMatchMode DescriptionMode { get; init; } = DescriptionMatchMode.Fuzzy;
}

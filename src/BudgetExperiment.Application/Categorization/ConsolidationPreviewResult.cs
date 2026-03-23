// <copyright file="ConsolidationPreviewResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// The result of previewing a consolidation suggestion against a sample of transaction descriptions.
/// </summary>
public sealed record ConsolidationPreviewResult
{
    /// <summary>
    /// Gets the total number of sample descriptions tested.
    /// </summary>
    public int TotalSamples { get; init; }

    /// <summary>
    /// Gets the number of sample descriptions that matched the merged pattern.
    /// </summary>
    public int MatchedSamples { get; init; }

    /// <summary>
    /// Gets the percentage of samples matched (0–100). Returns 0 when <see cref="TotalSamples"/> is 0.
    /// </summary>
    public double CoveragePercentage { get; init; }

    /// <summary>
    /// Gets the descriptions that matched the merged pattern.
    /// </summary>
    public IReadOnlyList<string> MatchedDescriptions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the descriptions that did not match the merged pattern.
    /// </summary>
    public IReadOnlyList<string> UnmatchedDescriptions { get; init; } = Array.Empty<string>();
}

// <copyright file="MatchingTolerancesDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for matching tolerances configuration.
/// </summary>
public sealed record MatchingTolerancesDto
{
    /// <summary>
    /// Gets or sets the maximum days before/after scheduled date to consider a match.
    /// </summary>
    public int DateToleranceDays { get; init; } = 7;

    /// <summary>
    /// Gets or sets the maximum percentage variance in amount (0.0 to 1.0).
    /// </summary>
    public decimal AmountTolerancePercent { get; init; } = 0.10m;

    /// <summary>
    /// Gets or sets the maximum absolute amount variance.
    /// </summary>
    public decimal AmountToleranceAbsolute { get; init; } = 10.00m;

    /// <summary>
    /// Gets or sets the minimum description similarity threshold (0.0 to 1.0).
    /// </summary>
    public decimal DescriptionSimilarityThreshold { get; init; } = 0.6m;

    /// <summary>
    /// Gets or sets the minimum confidence for auto-matching (0.0 to 1.0).
    /// </summary>
    public decimal AutoMatchThreshold { get; init; } = 0.85m;
}

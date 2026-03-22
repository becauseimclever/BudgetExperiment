// <copyright file="SuggestionTypeMetricsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing metrics for a specific suggestion type.
/// </summary>
public sealed record SuggestionTypeMetricsDto
{
    /// <summary>
    /// Gets the suggestion type name (e.g., "NewRule", "PatternOptimization", "Category").
    /// </summary>
    public required string Type
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of accepted suggestions of this type.
    /// </summary>
    public int Accepted
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of dismissed suggestions of this type.
    /// </summary>
    public int Dismissed
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of pending suggestions of this type.
    /// </summary>
    public int Pending
    {
        get; init;
    }

    /// <summary>
    /// Gets the acceptance rate for this type (accepted / (accepted + dismissed)), or null if no reviewed suggestions.
    /// </summary>
    public decimal? AcceptanceRate
    {
        get; init;
    }
}

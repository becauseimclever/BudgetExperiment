// <copyright file="SuggestionMetricsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing aggregated suggestion quality metrics.
/// </summary>
public sealed record SuggestionMetricsDto
{
    /// <summary>
    /// Gets the total number of suggestions ever generated.
    /// </summary>
    public int TotalGenerated
    {
        get; init;
    }

    /// <summary>
    /// Gets the total number of accepted suggestions.
    /// </summary>
    public int Accepted
    {
        get; init;
    }

    /// <summary>
    /// Gets the total number of dismissed suggestions.
    /// </summary>
    public int Dismissed
    {
        get; init;
    }

    /// <summary>
    /// Gets the total number of pending suggestions.
    /// </summary>
    public int Pending
    {
        get; init;
    }

    /// <summary>
    /// Gets the overall acceptance rate (accepted / (accepted + dismissed)), or null if no reviewed suggestions.
    /// </summary>
    public decimal? AcceptanceRate
    {
        get; init;
    }

    /// <summary>
    /// Gets the average confidence score of accepted suggestions, or null if none.
    /// </summary>
    public decimal? AverageAcceptedConfidence
    {
        get; init;
    }

    /// <summary>
    /// Gets the average confidence score of dismissed suggestions, or null if none.
    /// </summary>
    public decimal? AverageDismissedConfidence
    {
        get; init;
    }

    /// <summary>
    /// Gets the per-type metrics breakdown.
    /// </summary>
    public IReadOnlyList<SuggestionTypeMetricsDto> ByType { get; init; } = [];
}

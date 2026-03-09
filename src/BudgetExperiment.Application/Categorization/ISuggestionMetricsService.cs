// <copyright file="ISuggestionMetricsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for computing suggestion quality metrics.
/// </summary>
public interface ISuggestionMetricsService
{
    /// <summary>
    /// Gets aggregated suggestion quality metrics combining both rule and category suggestions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The aggregated metrics.</returns>
    Task<SuggestionMetricsDto> GetMetricsAsync(CancellationToken ct = default);
}

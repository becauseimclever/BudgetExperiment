// <copyright file="IRuleConsolidationPreviewService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Tests a consolidation suggestion's merged pattern against a sample of transaction descriptions
/// and returns coverage statistics.
/// </summary>
public interface IRuleConsolidationPreviewService
{
    /// <summary>
    /// Evaluates the merged pattern from a <see cref="ConsolidationSuggestion"/> against
    /// <paramref name="sampleDescriptions"/> and returns coverage statistics.
    /// </summary>
    /// <param name="suggestion">The consolidation suggestion whose merged pattern should be tested.</param>
    /// <param name="sampleDescriptions">The transaction descriptions to test against.</param>
    /// <returns>A <see cref="ConsolidationPreviewResult"/> with match and coverage statistics.</returns>
    Task<ConsolidationPreviewResult> PreviewConsolidationAsync(
        ConsolidationSuggestion suggestion,
        IReadOnlyList<string> sampleDescriptions);
}

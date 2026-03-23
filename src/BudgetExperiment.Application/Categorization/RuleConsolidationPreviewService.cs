// <copyright file="RuleConsolidationPreviewService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using BudgetExperiment.Domain.Categorization;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Evaluates a consolidation suggestion's merged pattern against sample descriptions
/// and returns coverage statistics.
/// </summary>
public sealed class RuleConsolidationPreviewService : IRuleConsolidationPreviewService
{
    /// <inheritdoc/>
    public Task<ConsolidationPreviewResult> PreviewConsolidationAsync(
        ConsolidationSuggestion suggestion,
        IReadOnlyList<string> sampleDescriptions)
    {
        var matched = sampleDescriptions
            .Where(d => Matches(suggestion.MergedMatchType, suggestion.MergedPattern, d))
            .ToList();

        var unmatched = sampleDescriptions
            .Where(d => !Matches(suggestion.MergedMatchType, suggestion.MergedPattern, d))
            .ToList();

        var total = sampleDescriptions.Count;
        var coverage = total == 0 ? 0.0 : (double)matched.Count / total * 100.0;

        return Task.FromResult(new ConsolidationPreviewResult
        {
            TotalSamples = total,
            MatchedSamples = matched.Count,
            CoveragePercentage = coverage,
            MatchedDescriptions = matched,
            UnmatchedDescriptions = unmatched,
        });
    }

    private static bool Matches(RuleMatchType matchType, string pattern, string description)
    {
        return matchType switch
        {
            RuleMatchType.Contains => description.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Exact => string.Equals(description, pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.StartsWith => description.StartsWith(pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.EndsWith => description.EndsWith(pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Regex => new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(description),
            _ => false,
        };
    }
}

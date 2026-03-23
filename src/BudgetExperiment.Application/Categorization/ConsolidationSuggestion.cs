// <copyright file="ConsolidationSuggestion.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Categorization;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a suggestion to consolidate two or more redundant categorization rules into one.
/// </summary>
public sealed record ConsolidationSuggestion
{
    /// <summary>
    /// Gets the IDs of the source rules that should be merged.
    /// </summary>
    public IReadOnlyList<Guid> SourceRuleIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets the pattern that the merged rule should use.
    /// </summary>
    public string MergedPattern { get; init; } = string.Empty;

    /// <summary>
    /// Gets the match type that the merged rule should use.
    /// </summary>
    public RuleMatchType MergedMatchType { get; init; }

    /// <summary>
    /// Gets the confidence score for this suggestion (0.0–1.0).
    /// </summary>
    public double Confidence { get; init; }
}

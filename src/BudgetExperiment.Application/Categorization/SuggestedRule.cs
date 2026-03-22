// <copyright file="SuggestedRule.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a suggested categorization rule.
/// </summary>
public sealed class SuggestedRule
{
    /// <summary>
    /// Gets the pattern to match against transaction descriptions.
    /// </summary>
    public required string Pattern
    {
        get; init;
    }

    /// <summary>
    /// Gets the suggested match type.
    /// </summary>
    public RuleMatchType MatchType
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of uncategorized transactions that would match.
    /// </summary>
    public int MatchingTransactionCount
    {
        get; init;
    }

    /// <summary>
    /// Gets sample transaction descriptions.
    /// </summary>
    public IReadOnlyList<string> SampleDescriptions { get; init; } = Array.Empty<string>();
}

// <copyright file="SuggestedCategoryRuleDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a suggested categorization rule.
/// </summary>
public sealed record SuggestedCategoryRuleDto
{
    /// <summary>
    /// Gets the pattern to match.
    /// </summary>
    public required string Pattern
    {
        get; init;
    }

    /// <summary>
    /// Gets the match type (Contains, StartsWith, etc.).
    /// </summary>
    public required string MatchType
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of transactions that would match this rule.
    /// </summary>
    public required int MatchingTransactionCount
    {
        get; init;
    }

    /// <summary>
    /// Gets sample transaction descriptions that would match.
    /// </summary>
    public required IReadOnlyList<string> SampleDescriptions
    {
        get; init;
    }
}

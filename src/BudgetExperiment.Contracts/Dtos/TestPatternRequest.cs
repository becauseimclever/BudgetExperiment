// <copyright file="TestPatternRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request for testing a pattern against existing transactions.
/// </summary>
public sealed class TestPatternRequest
{
    /// <summary>
    /// Gets or sets the pattern to test.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match type (Exact, Contains, StartsWith, EndsWith, Regex).
    /// </summary>
    public string MatchType { get; set; } = "Contains";

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of matching descriptions to return.
    /// </summary>
    public int Limit { get; set; } = 10;
}

// <copyright file="TestPatternResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response from testing a pattern.
/// </summary>
public sealed class TestPatternResponse
{
    /// <summary>
    /// Gets or sets the list of matching transaction descriptions.
    /// </summary>
    public IReadOnlyList<string> MatchingDescriptions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the total count of matches.
    /// </summary>
    public int MatchCount { get; set; }
}

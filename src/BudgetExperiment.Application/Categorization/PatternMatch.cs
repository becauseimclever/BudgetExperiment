// <copyright file="PatternMatch.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a pattern match from transaction analysis.
/// </summary>
public sealed class PatternMatch
{
    /// <summary>
    /// Gets the matched pattern (merchant keyword).
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the suggested category name.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the suggested icon.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Gets or sets the count of transactions matching this pattern.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets the sample transaction descriptions.
    /// </summary>
    public List<string> SampleDescriptions { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether this is from a learned mapping.
    /// </summary>
    public bool IsLearned { get; init; }
}

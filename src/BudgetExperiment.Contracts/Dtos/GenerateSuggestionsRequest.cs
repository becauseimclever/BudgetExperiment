// <copyright file="GenerateSuggestionsRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for generating AI suggestions.
/// </summary>
public sealed class GenerateSuggestionsRequest
{
    /// <summary>
    /// Gets or sets the suggestion type filter (NewRule, Optimization, Conflict, or null for all).
    /// </summary>
    public string? SuggestionType
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the maximum number of suggestions to generate.
    /// </summary>
    public int MaxSuggestions { get; set; } = 10;
}

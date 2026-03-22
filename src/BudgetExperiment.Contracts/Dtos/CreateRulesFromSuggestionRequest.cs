// <copyright file="CreateRulesFromSuggestionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to create rules from a category suggestion.
/// </summary>
public sealed record CreateRulesFromSuggestionRequest
{
    /// <summary>
    /// Gets the category ID to assign the rules to.
    /// </summary>
    public required Guid CategoryId
    {
        get; init;
    }

    /// <summary>
    /// Gets the patterns to create rules for. If null or empty, all patterns from the suggestion are used.
    /// </summary>
    public IReadOnlyList<string>? Patterns
    {
        get; init;
    }
}

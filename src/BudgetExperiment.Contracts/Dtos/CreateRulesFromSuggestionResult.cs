// <copyright file="CreateRulesFromSuggestionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of creating rules from a suggestion.
/// </summary>
public sealed record CreateRulesFromSuggestionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public required bool Success
    {
        get; init;
    }

    /// <summary>
    /// Gets the created rules.
    /// </summary>
    public IReadOnlyList<CategorizationRuleDto>? CreatedRules
    {
        get; init;
    }

    /// <summary>
    /// Gets any conflicting rules that were detected.
    /// </summary>
    public IReadOnlyList<CategorizationRuleDto>? ConflictingRules
    {
        get; init;
    }

    /// <summary>
    /// Gets the error message if failed.
    /// </summary>
    public string? ErrorMessage
    {
        get; init;
    }
}

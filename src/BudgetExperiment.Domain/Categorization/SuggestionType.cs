// <copyright file="SuggestionType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Defines the type of AI-generated rule suggestion.
/// </summary>
public enum SuggestionType
{
    /// <summary>
    /// Suggestion to create a new categorization rule.
    /// </summary>
    NewRule,

    /// <summary>
    /// Suggestion to optimize an existing rule's pattern.
    /// </summary>
    PatternOptimization,

    /// <summary>
    /// Suggestion to merge multiple rules into one.
    /// </summary>
    RuleConsolidation,

    /// <summary>
    /// Alert about conflicting rules that match the same transactions.
    /// </summary>
    RuleConflict,

    /// <summary>
    /// Alert about a rule that never matches any transactions.
    /// </summary>
    UnusedRule,
}

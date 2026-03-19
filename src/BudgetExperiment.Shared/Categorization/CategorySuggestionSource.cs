// <copyright file="CategorySuggestionSource.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Shared.Categorization;

/// <summary>
/// Indicates how a category suggestion was generated.
/// </summary>
public enum CategorySuggestionSource
{
    /// <summary>
    /// Suggestion was generated from MerchantKnowledgeBase or learned merchant mappings.
    /// </summary>
    PatternMatch = 0,

    /// <summary>
    /// Suggestion was discovered by AI analysis of unmatched transaction descriptions.
    /// </summary>
    AiDiscovered = 1,
}

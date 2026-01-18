// <copyright file="SuggestionStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines the review status of a rule suggestion.
/// </summary>
public enum SuggestionStatus
{
    /// <summary>
    /// Suggestion is awaiting user review.
    /// </summary>
    Pending,

    /// <summary>
    /// Suggestion was accepted and applied.
    /// </summary>
    Accepted,

    /// <summary>
    /// Suggestion was dismissed by the user.
    /// </summary>
    Dismissed,
}

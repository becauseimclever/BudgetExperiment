// <copyright file="MatchConfidenceLevel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents the confidence level of a reconciliation match.
/// </summary>
public enum MatchConfidenceLevel
{
    /// <summary>
    /// High confidence match - typically auto-accepted without user review.
    /// </summary>
    High = 0,

    /// <summary>
    /// Medium confidence match - suggested for user review.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Low confidence match - shown but not actively suggested.
    /// </summary>
    Low = 2,
}

// <copyright file="ReconciliationMatchStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents the status of a reconciliation match between an imported transaction and a recurring instance.
/// </summary>
public enum ReconciliationMatchStatus
{
    /// <summary>
    /// Match has been suggested and is awaiting user decision.
    /// </summary>
    Suggested = 0,

    /// <summary>
    /// User confirmed and accepted the match.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// User rejected the suggested match.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// System automatically matched due to high confidence score.
    /// </summary>
    AutoMatched = 3,
}

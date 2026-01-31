// <copyright file="MatchSource.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Indicates how a reconciliation match was created.
/// </summary>
public enum MatchSource
{
    /// <summary>
    /// Match was created automatically by the system's matching algorithm.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Match was created manually by the user.
    /// </summary>
    Manual = 1,
}

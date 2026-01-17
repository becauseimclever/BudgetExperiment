// <copyright file="BudgetScope.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents the scope of budget-related entities.
/// </summary>
public enum BudgetScope
{
    /// <summary>
    /// Shared scope visible to all authenticated users (household/family budget).
    /// </summary>
    Shared = 0,

    /// <summary>
    /// Personal scope visible only to the owning user.
    /// </summary>
    Personal = 1,
}

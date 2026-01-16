// <copyright file="CategoryType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines the type of a budget category.
/// </summary>
public enum CategoryType
{
    /// <summary>
    /// Spending category (negative transactions).
    /// </summary>
    Expense,

    /// <summary>
    /// Income category (positive transactions).
    /// </summary>
    Income,

    /// <summary>
    /// Internal transfers (excluded from budget calculations).
    /// </summary>
    Transfer,
}

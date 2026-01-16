// <copyright file="BudgetStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Indicates the status of budget progress for a category.
/// </summary>
public enum BudgetStatus
{
    /// <summary>
    /// Less than 80% of budget used.
    /// </summary>
    OnTrack,

    /// <summary>
    /// 80-99% of budget used.
    /// </summary>
    Warning,

    /// <summary>
    /// 100% or more of budget used.
    /// </summary>
    OverBudget,

    /// <summary>
    /// No budget target set for this category/month.
    /// </summary>
    NoBudgetSet,
}

// <copyright file="AllocationWarningType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines the types of warnings that can occur during paycheck allocation calculation.
/// </summary>
public enum AllocationWarningType
{
    /// <summary>
    /// The paycheck amount is less than the required allocation per paycheck.
    /// </summary>
    InsufficientIncome = 0,

    /// <summary>
    /// The annual income cannot cover the annual bills regardless of allocation strategy.
    /// </summary>
    CannotReconcile = 1,

    /// <summary>
    /// No recurring bills are configured for allocation.
    /// </summary>
    NoBillsConfigured = 2,

    /// <summary>
    /// No income/paycheck amount was provided to check against.
    /// </summary>
    NoIncomeConfigured = 3,
}

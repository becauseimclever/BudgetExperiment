// <copyright file="ReconciliationStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Constants;

/// <summary>
/// Reconciliation status string constants used across the application.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class ReconciliationStatus
{
    /// <summary>
    /// The instance has been matched to an imported transaction.
    /// </summary>
    public const string Matched = "Matched";

    /// <summary>
    /// The instance has a suggested match awaiting confirmation.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// The instance has no match and is missing from imported transactions.
    /// </summary>
    public const string Missing = "Missing";
}

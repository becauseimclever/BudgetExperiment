// <copyright file="CurrencyDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Default currency values used across the application.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class CurrencyDefaults
{
    /// <summary>
    /// The default currency code when no user preference is set ("USD").
    /// </summary>
    public const string DefaultCurrency = "USD";
}

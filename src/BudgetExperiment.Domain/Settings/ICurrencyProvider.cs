// <copyright file="ICurrencyProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Settings;

/// <summary>
/// Provides the active currency for the current user.
/// Returns the user's <see cref="UserSettings.PreferredCurrency"/> when set,
/// or falls back to a default (USD) when unset or unauthenticated.
/// </summary>
public interface ICurrencyProvider
{
    /// <summary>
    /// Gets the currency code for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ISO 4217 currency code (e.g., "USD", "EUR").</returns>
    Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default);
}

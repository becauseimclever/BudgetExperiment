// <copyright file="IBalanceCalculationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Service interface for calculating account balances.
/// </summary>
public interface IBalanceCalculationService
{
    /// <summary>
    /// Gets the total balance across all accounts (or a single account) up to but not including the specified date.
    /// </summary>
    /// <param name="date">The exclusive end date for balance calculation.</param>
    /// <param name="accountId">Optional account filter. If null, includes all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total balance as a <see cref="MoneyValue"/>.</returns>
    Task<MoneyValue> GetBalanceBeforeDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

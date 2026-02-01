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

    /// <summary>
    /// Gets the total balance across all accounts (or a single account) as of the specified date (inclusive).
    /// This includes initial balances for accounts where <c>InitialBalanceDate &lt;= date</c> and
    /// all transactions up to and including the specified date.
    /// </summary>
    /// <param name="date">The inclusive end date for balance calculation.</param>
    /// <param name="accountId">Optional account filter. If null, includes all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total balance as a <see cref="MoneyValue"/>.</returns>
    Task<MoneyValue> GetBalanceAsOfDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the opening balance for a specific date. This includes:
    /// - Initial balances for accounts where <c>InitialBalanceDate &lt;= date</c> (accounts that exist as of this date)
    /// - All transactions that occurred BEFORE the specified date (not including transactions on the date itself)
    /// This is useful for calendar views where you want the balance at the START of a day.
    /// </summary>
    /// <param name="date">The date to get the opening balance for.</param>
    /// <param name="accountId">Optional account filter. If null, includes all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The opening balance as a <see cref="MoneyValue"/>.</returns>
    Task<MoneyValue> GetOpeningBalanceForDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets initial balances for accounts that start within the specified date range.
    /// This is useful for calendar views where accounts may start within the visible grid.
    /// </summary>
    /// <param name="startDate">The start of the date range (inclusive).</param>
    /// <param name="endDate">The end of the date range (inclusive).</param>
    /// <param name="accountId">Optional account filter. If null, includes all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping dates to the total initial balance amount for accounts starting on that date.</returns>
    Task<Dictionary<DateOnly, decimal>> GetInitialBalancesByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

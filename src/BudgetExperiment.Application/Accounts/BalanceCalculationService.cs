// <copyright file="BalanceCalculationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Service for calculating account balances.
/// </summary>
public sealed class BalanceCalculationService : IBalanceCalculationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="BalanceCalculationService"/> class.
    /// </summary>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    public BalanceCalculationService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc/>
    public async Task<MoneyValue> GetBalanceBeforeDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await GetAccountsAsync(accountId, cancellationToken);

        if (accounts.Count == 0)
        {
            return MoneyValue.Zero("USD");
        }

        // Sum initial balances only for accounts that started before the target date
        var initialBalanceSum = accounts
            .Where(a => a.InitialBalanceDate < date)
            .Sum(a => a.InitialBalance.Amount);

        // Sum all transactions before the date for each relevant account
        var transactionSum = 0m;

        foreach (var account in accounts.Where(a => a.InitialBalanceDate < date))
        {
            var transactions = await _transactionRepository.GetByDateRangeAsync(
                account.InitialBalanceDate,
                date.AddDays(-1),
                account.Id,
                cancellationToken);

            transactionSum += transactions.Sum(t => t.Amount.Amount);
        }

        return MoneyValue.Create("USD", initialBalanceSum + transactionSum);
    }

    /// <inheritdoc/>
    public async Task<MoneyValue> GetBalanceAsOfDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await GetAccountsAsync(accountId, cancellationToken);

        if (accounts.Count == 0)
        {
            return MoneyValue.Zero("USD");
        }

        // Sum initial balances for accounts that started on or before the target date
        var initialBalanceSum = accounts
            .Where(a => a.InitialBalanceDate <= date)
            .Sum(a => a.InitialBalance.Amount);

        // Sum all transactions up to and including the date for each relevant account
        var transactionSum = 0m;

        foreach (var account in accounts.Where(a => a.InitialBalanceDate <= date))
        {
            var transactions = await _transactionRepository.GetByDateRangeAsync(
                account.InitialBalanceDate,
                date,
                account.Id,
                cancellationToken);

            transactionSum += transactions.Sum(t => t.Amount.Amount);
        }

        return MoneyValue.Create("USD", initialBalanceSum + transactionSum);
    }

    /// <inheritdoc/>
    public async Task<MoneyValue> GetOpeningBalanceForDateAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await GetAccountsAsync(accountId, cancellationToken);

        if (accounts.Count == 0)
        {
            return MoneyValue.Zero("USD");
        }

        // Include initial balances for accounts that started BEFORE this date
        // Accounts starting ON or AFTER this date are handled separately via GetInitialBalancesByDateRangeAsync
        var relevantAccounts = accounts.Where(a => a.InitialBalanceDate < date).ToList();

        var initialBalanceSum = relevantAccounts.Sum(a => a.InitialBalance.Amount);

        // Sum all transactions BEFORE the date (not including transactions on the date itself)
        var transactionSum = 0m;

        foreach (var account in relevantAccounts)
        {
            var transactions = await _transactionRepository.GetByDateRangeAsync(
                account.InitialBalanceDate,
                date.AddDays(-1),
                account.Id,
                cancellationToken);

            transactionSum += transactions.Sum(t => t.Amount.Amount);
        }

        return MoneyValue.Create("USD", initialBalanceSum + transactionSum);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<DateOnly, decimal>> GetInitialBalancesByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await GetAccountsAsync(accountId, cancellationToken);

        // Find accounts that start within the date range
        var accountsStartingInRange = accounts
            .Where(a => a.InitialBalanceDate >= startDate && a.InitialBalanceDate <= endDate)
            .ToList();

        // Group by InitialBalanceDate and sum the initial balances
        var result = accountsStartingInRange
            .GroupBy(a => a.InitialBalanceDate)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.InitialBalance.Amount));

        return result;
    }

    private async Task<IReadOnlyList<Account>> GetAccountsAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(accountId.Value, cancellationToken);
            return account is null ? [] : [account];
        }

        return await _accountRepository.GetAllAsync(cancellationToken);
    }
}

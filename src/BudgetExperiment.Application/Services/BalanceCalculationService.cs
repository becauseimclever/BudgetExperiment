// <copyright file="BalanceCalculationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

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

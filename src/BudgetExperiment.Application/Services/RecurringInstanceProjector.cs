// <copyright file="RecurringInstanceProjector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Projects recurring transaction instances for date ranges.
/// </summary>
public sealed class RecurringInstanceProjector : IRecurringInstanceProjector
{
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringInstanceProjector"/> class.
    /// </summary>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    public RecurringInstanceProjector(
        IRecurringTransactionRepository recurringRepository,
        IAccountRepository accountRepository)
    {
        _recurringRepository = recurringRepository;
        _accountRepository = accountRepository;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<DateOnly, List<RecurringInstanceInfo>>> GetInstancesByDateRangeAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<DateOnly, List<RecurringInstanceInfo>>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var recurring in recurringTransactions.Where(r => r.IsActive))
        {
            var occurrences = recurring.GetOccurrencesBetween(fromDate, toDate);
            var exceptions = await _recurringRepository.GetExceptionsByDateRangeAsync(
                recurring.Id,
                fromDate,
                toDate,
                cancellationToken);
            var exceptionMap = exceptions.ToDictionary(e => e.OriginalDate);

            foreach (var date in occurrences)
            {
                exceptionMap.TryGetValue(date, out var exception);

                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                var instance = new RecurringInstanceInfo(
                    RecurringTransactionId: recurring.Id,
                    InstanceDate: date,
                    AccountId: recurring.AccountId,
                    AccountName: accountMap.GetValueOrDefault(recurring.AccountId, string.Empty),
                    Description: exception?.ModifiedDescription ?? recurring.Description,
                    Amount: exception?.ModifiedAmount ?? recurring.Amount,
                    CategoryId: recurring.CategoryId,
                    CategoryName: recurring.Category?.Name,
                    IsModified: exception?.ExceptionType == ExceptionType.Modified,
                    IsSkipped: false);

                if (!result.TryGetValue(date, out var list))
                {
                    list = new List<RecurringInstanceInfo>();
                    result[date] = list;
                }

                list.Add(instance);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<List<RecurringInstanceInfo>> GetInstancesForDateAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var result = new List<RecurringInstanceInfo>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var recurring in recurringTransactions.Where(r => r.IsActive))
        {
            var occurrences = recurring.GetOccurrencesBetween(date, date);
            if (!occurrences.Contains(date))
            {
                continue;
            }

            var exception = await _recurringRepository.GetExceptionAsync(recurring.Id, date, cancellationToken);

            result.Add(new RecurringInstanceInfo(
                RecurringTransactionId: recurring.Id,
                InstanceDate: date,
                AccountId: recurring.AccountId,
                AccountName: accountMap.GetValueOrDefault(recurring.AccountId, string.Empty),
                Description: exception?.ModifiedDescription ?? recurring.Description,
                Amount: exception?.ModifiedAmount ?? recurring.Amount,
                CategoryId: recurring.CategoryId,
                CategoryName: recurring.Category?.Name,
                IsModified: exception?.ExceptionType == ExceptionType.Modified,
                IsSkipped: exception?.ExceptionType == ExceptionType.Skipped));
        }

        return result;
    }
}

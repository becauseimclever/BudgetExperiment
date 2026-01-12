// <copyright file="PastDueService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for past-due recurring item operations.
/// </summary>
public sealed class PastDueService : IPastDueService
{
    private const int LookbackDays = 30;

    private readonly IRecurringTransactionRepository _recurringTransactionRepo;
    private readonly IRecurringTransferRepository _recurringTransferRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly Func<DateOnly> _todayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PastDueService"/> class.
    /// </summary>
    /// <param name="recurringTransactionRepo">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepo">The recurring transfer repository.</param>
    /// <param name="transactionRepo">The transaction repository.</param>
    /// <param name="accountRepo">The account repository.</param>
    /// <param name="todayProvider">Optional function to provide current date (for testing).</param>
    public PastDueService(
        IRecurringTransactionRepository recurringTransactionRepo,
        IRecurringTransferRepository recurringTransferRepo,
        ITransactionRepository transactionRepo,
        IAccountRepository accountRepo,
        Func<DateOnly>? todayProvider = null)
    {
        this._recurringTransactionRepo = recurringTransactionRepo;
        this._recurringTransferRepo = recurringTransferRepo;
        this._transactionRepo = transactionRepo;
        this._accountRepo = accountRepo;
        this._todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <inheritdoc/>
    public async Task<PastDueSummaryDto> GetPastDueItemsAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var today = this._todayProvider();
        var lookbackDate = today.AddDays(-LookbackDays);
        var yesterday = today.AddDays(-1);

        var items = new List<PastDueItemDto>();

        // Get recurring transactions
        var recurringTransactions = accountId.HasValue
            ? await this._recurringTransactionRepo.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await this._recurringTransactionRepo.GetActiveAsync(cancellationToken);

        foreach (var recurring in recurringTransactions)
        {
            var occurrences = recurring.GetOccurrencesBetween(lookbackDate, yesterday);
            foreach (var date in occurrences)
            {
                // Check if skipped
                var exception = await this._recurringTransactionRepo.GetExceptionAsync(recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Check if realized
                var realized = await this._transactionRepo.GetByRecurringInstanceAsync(recurring.Id, date, cancellationToken);
                if (realized != null)
                {
                    continue;
                }

                // Get account info
                var account = await this._accountRepo.GetByIdAsync(recurring.AccountId, cancellationToken);

                items.Add(new PastDueItemDto
                {
                    Id = recurring.Id,
                    Type = "recurring-transaction",
                    InstanceDate = date,
                    DaysPastDue = today.DayNumber - date.DayNumber,
                    Description = recurring.Description,
                    Amount = new MoneyDto { Currency = recurring.Amount.Currency, Amount = recurring.Amount.Amount },
                    AccountId = recurring.AccountId,
                    AccountName = account?.Name,
                });
            }
        }

        // Get recurring transfers
        var recurringTransfers = accountId.HasValue
            ? await this._recurringTransferRepo.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await this._recurringTransferRepo.GetActiveAsync(cancellationToken);

        foreach (var recurring in recurringTransfers)
        {
            var occurrences = recurring.GetOccurrencesBetween(lookbackDate, yesterday);
            foreach (var date in occurrences)
            {
                // Check if skipped
                var exception = await this._recurringTransferRepo.GetExceptionAsync(recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Check if realized
                var realized = await this._transactionRepo.GetByRecurringTransferInstanceAsync(recurring.Id, date, cancellationToken);
                if (realized.Count > 0)
                {
                    continue;
                }

                // Get account info
                var sourceAccount = await this._accountRepo.GetByIdAsync(recurring.SourceAccountId, cancellationToken);
                var destAccount = await this._accountRepo.GetByIdAsync(recurring.DestinationAccountId, cancellationToken);

                items.Add(new PastDueItemDto
                {
                    Id = recurring.Id,
                    Type = "recurring-transfer",
                    InstanceDate = date,
                    DaysPastDue = today.DayNumber - date.DayNumber,
                    Description = recurring.Description,
                    Amount = new MoneyDto { Currency = recurring.Amount.Currency, Amount = recurring.Amount.Amount },
                    SourceAccountId = recurring.SourceAccountId,
                    SourceAccountName = sourceAccount?.Name,
                    DestinationAccountId = recurring.DestinationAccountId,
                    DestinationAccountName = destAccount?.Name,
                });
            }
        }

        // Sort by instance date (oldest first)
        var sortedItems = items.OrderBy(i => i.InstanceDate).ToList();

        // Calculate total amount (only for same currency - default to USD for now)
        var totalAmount = items.Count > 0
            ? items.Sum(i => i.Amount.Amount)
            : 0m;

        return new PastDueSummaryDto
        {
            Items = sortedItems,
            TotalCount = sortedItems.Count,
            OldestDate = sortedItems.Count > 0 ? sortedItems[0].InstanceDate : null,
            TotalAmount = sortedItems.Count > 0
                ? new MoneyDto { Currency = "USD", Amount = totalAmount }
                : null,
        };
    }

    /// <inheritdoc/>
    public Task<BatchRealizeResultDto> RealizeBatchAsync(BatchRealizeRequest request, CancellationToken cancellationToken = default)
    {
        // This will be implemented when we wire up the batch endpoint
        throw new NotImplementedException();
    }
}

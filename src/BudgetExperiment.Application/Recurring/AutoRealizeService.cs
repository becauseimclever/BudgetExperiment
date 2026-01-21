// <copyright file="AutoRealizeService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service for auto-realizing past-due recurring items.
/// </summary>
public sealed class AutoRealizeService : IAutoRealizeService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRealizeService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="settingsRepository">The app settings repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public AutoRealizeService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAppSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<int> AutoRealizePastDueItemsIfEnabledAsync(
        DateOnly today,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (!settings.AutoRealizePastDueItems)
        {
            return 0;
        }

        var lookbackDate = today.AddDays(-settings.PastDueLookbackDays);
        var yesterday = today.AddDays(-1);

        var realizedCount = 0;

        // Auto-realize recurring transactions
        realizedCount += await AutoRealizeRecurringTransactionsAsync(
            lookbackDate, yesterday, accountId, cancellationToken);

        // Auto-realize recurring transfers
        realizedCount += await AutoRealizeRecurringTransfersAsync(
            lookbackDate, yesterday, accountId, cancellationToken);

        if (realizedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return realizedCount;
    }

    private async Task<IReadOnlyList<RecurringTransaction>> GetRecurringTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        return accountId.HasValue
            ? await _recurringRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringRepository.GetActiveAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RecurringTransfer>> GetRecurringTransfersAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        return accountId.HasValue
            ? await _recurringTransferRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringTransferRepository.GetActiveAsync(cancellationToken);
    }

    private async Task<int> AutoRealizeRecurringTransactionsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var realizedCount = 0;

        foreach (var recurring in recurringTransactions.Where(r => r.IsActive))
        {
            var occurrences = recurring.GetOccurrencesBetween(fromDate, toDate);

            foreach (var date in occurrences)
            {
                // Skip if already realized
                var existing = await _transactionRepository.GetByRecurringInstanceAsync(
                    recurring.Id, date, cancellationToken);
                if (existing != null)
                {
                    continue;
                }

                // Check for exceptions (skipped or modified)
                var exception = await _recurringRepository.GetExceptionAsync(
                    recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Realize the instance
                var amount = exception?.ModifiedAmount ?? recurring.Amount;
                var description = exception?.ModifiedDescription ?? recurring.Description;
                var actualDate = exception?.ModifiedDate ?? date;

                var transaction = Transaction.CreateFromRecurring(
                    recurring.AccountId,
                    amount,
                    actualDate,
                    description,
                    recurring.Id,
                    date);

                await _transactionRepository.AddAsync(transaction, cancellationToken);
                realizedCount++;
            }
        }

        return realizedCount;
    }

    private async Task<int> AutoRealizeRecurringTransfersAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);
        var realizedCount = 0;

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            // Skip if filtering by account and this transfer doesn't involve that account
            if (accountId.HasValue &&
                transfer.SourceAccountId != accountId.Value &&
                transfer.DestinationAccountId != accountId.Value)
            {
                continue;
            }

            var occurrences = transfer.GetOccurrencesBetween(fromDate, toDate);

            foreach (var date in occurrences)
            {
                // Skip if already realized
                var existing = await _transactionRepository.GetByRecurringTransferInstanceAsync(
                    transfer.Id, date, cancellationToken);
                if (existing.Count > 0)
                {
                    continue;
                }

                // Check for exceptions (skipped or modified)
                var exception = await _recurringTransferRepository.GetExceptionAsync(
                    transfer.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Realize the transfer (creates two transactions - source and destination)
                var amount = exception?.ModifiedAmount ?? transfer.Amount;
                var description = exception?.ModifiedDescription ?? transfer.Description;
                var actualDate = exception?.ModifiedDate ?? date;
                var transferId = Guid.NewGuid();

                // Source transaction (negative)
                var sourceTransaction = Transaction.CreateFromRecurringTransfer(
                    transfer.SourceAccountId,
                    MoneyValue.Create(amount.Currency, -amount.Amount),
                    actualDate,
                    description,
                    transferId,
                    TransferDirection.Source,
                    transfer.Id,
                    date);
                await _transactionRepository.AddAsync(sourceTransaction, cancellationToken);
                realizedCount++;

                // Destination transaction (positive)
                var destTransaction = Transaction.CreateFromRecurringTransfer(
                    transfer.DestinationAccountId,
                    amount,
                    actualDate,
                    description,
                    transferId,
                    TransferDirection.Destination,
                    transfer.Id,
                    date);
                await _transactionRepository.AddAsync(destTransaction, cancellationToken);
                realizedCount++;
            }
        }

        return realizedCount;
    }
}

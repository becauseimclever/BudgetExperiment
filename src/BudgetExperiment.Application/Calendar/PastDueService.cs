// <copyright file="PastDueService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Calendar;

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
    private readonly IRecurringTransactionRealizationService _transactionRealizationService;
    private readonly IRecurringTransferRealizationService _transferRealizationService;
    private readonly ICurrencyProvider _currencyProvider;
    private readonly Func<DateOnly> _todayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PastDueService"/> class.
    /// </summary>
    /// <param name="recurringTransactionRepo">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepo">The recurring transfer repository.</param>
    /// <param name="transactionRepo">The transaction repository.</param>
    /// <param name="accountRepo">The account repository.</param>
    /// <param name="transactionRealizationService">The recurring transaction realization service.</param>
    /// <param name="transferRealizationService">The recurring transfer realization service.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="todayProvider">Optional function to provide current date (for testing).</param>
    public PastDueService(
        IRecurringTransactionRepository recurringTransactionRepo,
        IRecurringTransferRepository recurringTransferRepo,
        ITransactionRepository transactionRepo,
        IAccountRepository accountRepo,
        IRecurringTransactionRealizationService transactionRealizationService,
        IRecurringTransferRealizationService transferRealizationService,
        ICurrencyProvider currencyProvider,
        Func<DateOnly>? todayProvider = null)
    {
        _recurringTransactionRepo = recurringTransactionRepo;
        _recurringTransferRepo = recurringTransferRepo;
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _transactionRealizationService = transactionRealizationService;
        _transferRealizationService = transferRealizationService;
        _currencyProvider = currencyProvider;
        _todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <inheritdoc/>
    public async Task<PastDueSummaryDto> GetPastDueItemsAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var today = _todayProvider();
        var lookbackDate = today.AddDays(-LookbackDays);
        var yesterday = today.AddDays(-1);

        var items = new List<PastDueItemDto>();

        // Get recurring transactions
        var recurringTransactions = accountId.HasValue
            ? await _recurringTransactionRepo.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringTransactionRepo.GetActiveAsync(cancellationToken);

        foreach (var recurring in recurringTransactions)
        {
            var occurrences = recurring.GetOccurrencesBetween(lookbackDate, yesterday);
            foreach (var date in occurrences)
            {
                // Check if skipped
                var exception = await _recurringTransactionRepo.GetExceptionAsync(recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Check if realized
                var realized = await _transactionRepo.GetByRecurringInstanceAsync(recurring.Id, date, cancellationToken);
                if (realized != null)
                {
                    continue;
                }

                // Get account info
                var account = await _accountRepo.GetByIdAsync(recurring.AccountId, cancellationToken);

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
            ? await _recurringTransferRepo.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringTransferRepo.GetActiveAsync(cancellationToken);

        foreach (var recurring in recurringTransfers)
        {
            var occurrences = recurring.GetOccurrencesBetween(lookbackDate, yesterday);
            foreach (var date in occurrences)
            {
                // Check if skipped
                var exception = await _recurringTransferRepo.GetExceptionAsync(recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Check if realized
                var realized = await _transactionRepo.GetByRecurringTransferInstanceAsync(recurring.Id, date, cancellationToken);
                if (realized.Count > 0)
                {
                    continue;
                }

                // Get account info
                var sourceAccount = await _accountRepo.GetByIdAsync(recurring.SourceAccountId, cancellationToken);
                var destAccount = await _accountRepo.GetByIdAsync(recurring.DestinationAccountId, cancellationToken);

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

        // Calculate total amount using global currency
        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var totalAmount = items.Count > 0
            ? items.Sum(i => i.Amount.Amount)
            : 0m;

        return new PastDueSummaryDto
        {
            Items = sortedItems,
            TotalCount = sortedItems.Count,
            OldestDate = sortedItems.Count > 0 ? sortedItems[0].InstanceDate : null,
            TotalAmount = sortedItems.Count > 0
                ? new MoneyDto { Currency = currency, Amount = totalAmount }
                : null,
        };
    }

    /// <inheritdoc/>
    public async Task<BatchRealizeResultDto> RealizeBatchAsync(BatchRealizeRequest request, CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        var failures = new List<BatchRealizeFailure>();

        foreach (var item in request.Items)
        {
            try
            {
                if (item.Type == "recurring-transaction")
                {
                    var realizeRequest = new RealizeRecurringTransactionRequest
                    {
                        InstanceDate = item.InstanceDate,
                    };
                    await _transactionRealizationService.RealizeInstanceAsync(
                        item.Id,
                        realizeRequest,
                        cancellationToken);
                    successCount++;
                }
                else if (item.Type == "recurring-transfer")
                {
                    var realizeRequest = new RealizeRecurringTransferRequest
                    {
                        InstanceDate = item.InstanceDate,
                    };
                    await _transferRealizationService.RealizeInstanceAsync(
                        item.Id,
                        realizeRequest,
                        cancellationToken);
                    successCount++;
                }
                else
                {
                    failures.Add(new BatchRealizeFailure
                    {
                        Id = item.Id,
                        Type = item.Type,
                        InstanceDate = item.InstanceDate,
                        Error = $"Unknown item type: {item.Type}",
                    });
                }
            }
            catch (Exception ex)
            {
                failures.Add(new BatchRealizeFailure
                {
                    Id = item.Id,
                    Type = item.Type,
                    InstanceDate = item.InstanceDate,
                    Error = ex.Message,
                });
            }
        }

        return new BatchRealizeResultDto
        {
            SuccessCount = successCount,
            FailureCount = failures.Count,
            Failures = failures,
        };
    }
}

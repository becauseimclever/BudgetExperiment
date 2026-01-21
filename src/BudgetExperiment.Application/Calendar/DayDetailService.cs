// <copyright file="DayDetailService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Service for building day detail views.
/// </summary>
public sealed class DayDetailService : IDayDetailService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IRecurringInstanceProjector _recurringInstanceProjector;
    private readonly IRecurringTransferInstanceProjector _recurringTransferInstanceProjector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DayDetailService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="recurringInstanceProjector">The recurring instance projector.</param>
    /// <param name="recurringTransferInstanceProjector">The recurring transfer instance projector.</param>
    public DayDetailService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
    }

    /// <inheritdoc/>
    public async Task<DayDetailDto> GetDayDetailAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        // Fetch data sequentially (DbContext is not thread-safe for concurrent operations)
        var transactions = await _transactionRepository.GetByDateRangeAsync(date, date, accountId, cancellationToken);
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);

        // Get recurring instances for this specific date using the projector
        var recurringInstances = await _recurringInstanceProjector.GetInstancesForDateAsync(
            recurringTransactions,
            date,
            cancellationToken);

        // Get recurring transfer instances for this specific date using the projector
        var recurringTransferInstances = await _recurringTransferInstanceProjector.GetInstancesForDateAsync(
            recurringTransfers,
            date,
            accountId,
            cancellationToken);

        // Build items list
        var items = new List<DayDetailItemDto>();

        // Add actual transactions
        foreach (var txn in transactions)
        {
            items.Add(new DayDetailItemDto
            {
                Id = txn.Id,
                Type = "transaction",
                Description = txn.Description,
                Amount = DomainToDtoMapper.ToDto(txn.Amount),
                CategoryId = txn.CategoryId,
                AccountName = accountMap.GetValueOrDefault(txn.AccountId, string.Empty),
                AccountId = txn.AccountId,
                CreatedAt = txn.CreatedAt,
                IsModified = false,
                IsSkipped = false,
                RecurringTransactionId = txn.RecurringTransactionId,
                RecurringTransferId = txn.RecurringTransferId,
                IsTransfer = txn.IsTransfer,
                TransferId = txn.TransferId,
                TransferDirection = txn.TransferDirection?.ToString(),
            });
        }

        // Add recurring instances (excluding those that might already be realized)
        foreach (var instance in recurringInstances)
        {
            // Check if there's already a transaction for this recurring instance
            var hasRealized = transactions.Any(t =>
                t.RecurringTransactionId == instance.RecurringTransactionId &&
                t.RecurringInstanceDate == date);

            if (!hasRealized && !instance.IsSkipped)
            {
                items.Add(new DayDetailItemDto
                {
                    Id = Guid.NewGuid(), // Temporary ID for display
                    Type = "recurring",
                    Description = instance.Description,
                    Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
                    CategoryId = instance.CategoryId,
                    AccountName = instance.AccountName,
                    AccountId = instance.AccountId,
                    CreatedAt = null,
                    IsModified = instance.IsModified,
                    IsSkipped = instance.IsSkipped,
                    RecurringTransactionId = instance.RecurringTransactionId,
                    IsTransfer = false,
                    TransferId = null,
                    TransferDirection = null,
                });
            }
        }

        // Add recurring transfer instances (excluding those that might already be realized)
        foreach (var instance in recurringTransferInstances)
        {
            // Check if there's already a transaction for this recurring transfer instance
            var hasRealized = transactions.Any(t =>
                t.RecurringTransferId == instance.RecurringTransferId &&
                t.RecurringTransferInstanceDate == date);

            if (!hasRealized && !instance.IsSkipped)
            {
                items.Add(new DayDetailItemDto
                {
                    Id = Guid.NewGuid(), // Temporary ID for display
                    Type = "recurring-transfer",
                    Description = instance.Description,
                    Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
                    CategoryId = null,
                    AccountName = instance.AccountName,
                    AccountId = instance.AccountId,
                    CreatedAt = null,
                    IsModified = instance.IsModified,
                    IsSkipped = instance.IsSkipped,
                    RecurringTransactionId = null,
                    RecurringTransferId = instance.RecurringTransferId,
                    IsTransfer = true,
                    TransferId = null,
                    TransferDirection = instance.TransferDirection,
                });
            }
        }

        // Calculate summary
        var actualTotal = items.Where(i => i.Type == "transaction").Sum(i => i.Amount.Amount);
        var projectedTotal = items.Where(i => i.Type == "recurring" || i.Type == "recurring-transfer").Sum(i => i.Amount.Amount);

        return new DayDetailDto
        {
            Date = date,
            Items = items.OrderBy(i => i.Type).ThenBy(i => i.CreatedAt ?? DateTime.MaxValue).ToList(),
            Summary = new DayDetailSummaryDto
            {
                TotalActual = new MoneyDto { Currency = "USD", Amount = actualTotal },
                TotalProjected = new MoneyDto { Currency = "USD", Amount = projectedTotal },
                CombinedTotal = new MoneyDto { Currency = "USD", Amount = actualTotal + projectedTotal },
                ItemCount = items.Count,
            },
        };
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
}

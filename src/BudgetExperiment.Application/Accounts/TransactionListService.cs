// <copyright file="TransactionListService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Service for building account transaction lists.
/// </summary>
public sealed class TransactionListService : ITransactionListService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceCalculationService _balanceCalculationService;
    private readonly IRecurringInstanceProjector _recurringInstanceProjector;
    private readonly IRecurringTransferInstanceProjector _recurringTransferInstanceProjector;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionListService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="balanceCalculationService">The balance calculation service.</param>
    /// <param name="recurringInstanceProjector">The recurring instance projector.</param>
    /// <param name="recurringTransferInstanceProjector">The recurring transfer instance projector.</param>
    public TransactionListService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
        IBalanceCalculationService balanceCalculationService,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
        _balanceCalculationService = balanceCalculationService;
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
    }

    /// <inheritdoc/>
    public async Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true,
        CancellationToken cancellationToken = default)
    {
        // Get the account
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException($"Account with ID {accountId} not found.");

        // Get transactions for date range
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            accountId,
            cancellationToken);

        // Build items list
        var items = new List<TransactionListItemDto>();

        // Add actual transactions
        foreach (var txn in transactions)
        {
            items.Add(new TransactionListItemDto
            {
                Id = txn.Id,
                Type = "transaction",
                Date = txn.Date,
                Description = txn.Description,
                Amount = CommonMapper.ToDto(txn.Amount),
                CategoryId = txn.CategoryId,
                CategoryName = txn.Category?.Name,
                CreatedAt = txn.CreatedAt,
                IsModified = false,
                RecurringTransactionId = txn.RecurringTransactionId,
                RecurringTransferId = txn.RecurringTransferId,
                IsTransfer = txn.IsTransfer,
                TransferId = txn.TransferId,
                TransferDirection = txn.TransferDirection?.ToString(),
            });
        }

        // Add recurring instances if requested
        if (includeRecurring)
        {
            await AddRecurringTransactionInstancesAsync(accountId, startDate, endDate, transactions, items, cancellationToken);
            await AddRecurringTransferInstancesAsync(accountId, startDate, endDate, transactions, items, cancellationToken);
        }

        // Sort items by date descending, then by created timestamp
        var sortedItems = items.OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAt ?? DateTime.MinValue).ToList();

        // Calculate starting balance for the date range
        var startingBalance = await _balanceCalculationService.GetBalanceBeforeDateAsync(
            startDate,
            accountId,
            cancellationToken);

        // Sort items by date ascending for running balance calculation
        var sortedForBalance = items.OrderBy(i => i.Date).ThenBy(i => i.CreatedAt ?? DateTime.MinValue).ToList();

        // Calculate running balance for each item
        var runningBalance = startingBalance.Amount;
        foreach (var item in sortedForBalance)
        {
            runningBalance += item.Amount.Amount;
            item.RunningBalance = new MoneyDto { Currency = "USD", Amount = runningBalance };
        }

        // Calculate daily balance summaries
        var dailyBalances = CalculateDailyBalances(sortedForBalance, startingBalance.Amount);

        // Calculate summary
        var transactionCount = sortedItems.Count(i => i.Type == "transaction");
        var recurringCount = sortedItems.Count(i => i.Type == "recurring" || i.Type == "recurring-transfer");
        var totalAmount = sortedItems.Sum(i => i.Amount.Amount);
        var totalIncome = sortedItems.Where(i => i.Amount.Amount > 0).Sum(i => i.Amount.Amount);
        var totalExpenses = sortedItems.Where(i => i.Amount.Amount < 0).Sum(i => i.Amount.Amount);
        var currentBalance = account.InitialBalance.Amount + totalAmount;

        return new TransactionListDto
        {
            AccountId = accountId,
            AccountName = account.Name,
            StartDate = startDate,
            EndDate = endDate,
            InitialBalance = new MoneyDto { Currency = account.InitialBalance.Currency, Amount = account.InitialBalance.Amount },
            InitialBalanceDate = account.InitialBalanceDate,
            Items = sortedItems,
            Summary = new TransactionListSummaryDto
            {
                TotalAmount = new MoneyDto { Currency = "USD", Amount = totalAmount },
                TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
                TotalExpenses = new MoneyDto { Currency = "USD", Amount = totalExpenses },
                TransactionCount = transactionCount,
                RecurringCount = recurringCount,
                CurrentBalance = new MoneyDto { Currency = "USD", Amount = currentBalance },
            },
            DailyBalances = dailyBalances.OrderByDescending(d => d.Date).ToList(),
            StartingBalance = new MoneyDto { Currency = startingBalance.Currency, Amount = startingBalance.Amount },
        };
    }

    private async Task AddRecurringTransactionInstancesAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Transaction> transactions,
        List<TransactionListItemDto> items,
        CancellationToken cancellationToken)
    {
        var recurringTransactions = await _recurringRepository.GetByAccountIdAsync(accountId, cancellationToken);
        var recurringInstances = await _recurringInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            startDate,
            endDate,
            cancellationToken);

        // Add recurring instances (excluding those that already have realized transactions)
        foreach (var (date, instances) in recurringInstances)
        {
            foreach (var instance in instances.Where(i => i.AccountId == accountId))
            {
                // Check if there's already a transaction for this recurring instance on this date
                var hasRealized = transactions.Any(t =>
                    t.RecurringTransactionId == instance.RecurringTransactionId &&
                    t.Date == date);

                if (!hasRealized)
                {
                    items.Add(new TransactionListItemDto
                    {
                        Id = instance.RecurringTransactionId,
                        Type = "recurring",
                        Date = date,
                        Description = instance.Description,
                        Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
                        CategoryId = instance.CategoryId,
                        CategoryName = instance.CategoryName,
                        CreatedAt = null,
                        IsModified = instance.IsModified,
                        RecurringTransactionId = instance.RecurringTransactionId,
                        RecurringTransferId = null,
                        IsTransfer = false,
                        TransferId = null,
                        TransferDirection = null,
                    });
                }
            }
        }
    }

    private async Task AddRecurringTransferInstancesAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Transaction> transactions,
        List<TransactionListItemDto> items,
        CancellationToken cancellationToken)
    {
        var recurringTransfers = await _recurringTransferRepository.GetByAccountIdAsync(accountId, cancellationToken);
        var recurringTransferInstances = await _recurringTransferInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransfers,
            startDate,
            endDate,
            accountId,
            cancellationToken);

        foreach (var (date, instances) in recurringTransferInstances)
        {
            foreach (var instance in instances)
            {
                // Check if there's already a transaction for this recurring transfer instance on this date
                var hasRealized = transactions.Any(t =>
                    t.RecurringTransferId == instance.RecurringTransferId &&
                    t.Date == date);

                if (!hasRealized)
                {
                    items.Add(new TransactionListItemDto
                    {
                        Id = instance.RecurringTransferId,
                        Type = "recurring-transfer",
                        Date = date,
                        Description = instance.Description,
                        Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
                        CategoryId = null,
                        CreatedAt = null,
                        IsModified = instance.IsModified,
                        RecurringTransactionId = null,
                        RecurringTransferId = instance.RecurringTransferId,
                        IsTransfer = true,
                        TransferId = null,
                        TransferDirection = instance.TransferDirection,
                    });
                }
            }
        }
    }

    private static List<DailyBalanceSummaryDto> CalculateDailyBalances(
        List<TransactionListItemDto> sortedForBalance,
        decimal startingBalanceAmount)
    {
        var dailyBalances = new List<DailyBalanceSummaryDto>();
        var dayBalance = startingBalanceAmount;

        foreach (var dayGroup in sortedForBalance.GroupBy(i => i.Date).OrderBy(g => g.Key))
        {
            var dayStart = dayBalance;
            var dayTotal = dayGroup.Sum(i => i.Amount.Amount);
            dayBalance += dayTotal;

            dailyBalances.Add(new DailyBalanceSummaryDto
            {
                Date = dayGroup.Key,
                StartingBalance = new MoneyDto { Currency = "USD", Amount = dayStart },
                EndingBalance = new MoneyDto { Currency = "USD", Amount = dayBalance },
                DayTotal = new MoneyDto { Currency = "USD", Amount = dayTotal },
                TransactionCount = dayGroup.Count(),
            });
        }

        return dailyBalances;
    }
}

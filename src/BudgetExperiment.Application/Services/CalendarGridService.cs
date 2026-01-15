// <copyright file="CalendarGridService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for building complete calendar grid views with all data pre-computed.
/// </summary>
public sealed class CalendarGridService : ICalendarGridService
{
    private const int GridDays = 42; // 6 weeks * 7 days

    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceCalculationService _balanceCalculationService;
    private readonly IRecurringInstanceProjector _recurringInstanceProjector;
    private readonly IRecurringTransferInstanceProjector _recurringTransferInstanceProjector;
    private readonly IAutoRealizeService _autoRealizeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarGridService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="balanceCalculationService">The balance calculation service.</param>
    /// <param name="recurringInstanceProjector">The recurring instance projector.</param>
    /// <param name="recurringTransferInstanceProjector">The recurring transfer instance projector.</param>
    /// <param name="autoRealizeService">The auto-realize service.</param>
    public CalendarGridService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
        IBalanceCalculationService balanceCalculationService,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector,
        IAutoRealizeService autoRealizeService)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
        _balanceCalculationService = balanceCalculationService;
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
        _autoRealizeService = autoRealizeService;
    }

    /// <inheritdoc/>
    public async Task<CalendarGridDto> GetCalendarGridAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var firstOfMonth = new DateOnly(year, month, 1);
        var startDayOfWeek = (int)firstOfMonth.DayOfWeek;
        var gridStartDate = firstOfMonth.AddDays(-startDayOfWeek);
        var gridEndDate = gridStartDate.AddDays(GridDays - 1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // Auto-realize past-due items if setting is enabled
        await _autoRealizeService.AutoRealizePastDueItemsIfEnabledAsync(today, accountId, cancellationToken);

        // Fetch data sequentially (DbContext is not thread-safe for concurrent operations)
        var dailyTotalsList = await _transactionRepository.GetDailyTotalsAsync(year, month, accountId, cancellationToken);
        var dailyTotals = dailyTotalsList.ToDictionary(d => d.Date);
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);

        // Project recurring instances for the grid date range using the projector
        var recurringByDate = await _recurringInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            gridStartDate,
            gridEndDate,
            cancellationToken);

        // Project recurring transfer instances for the grid date range using the projector
        var recurringTransfersByDate = await _recurringTransferInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransfers,
            gridStartDate,
            gridEndDate,
            accountId,
            cancellationToken);

        // Build grid days
        var days = new List<CalendarDaySummaryDto>(GridDays);
        for (int i = 0; i < GridDays; i++)
        {
            var date = gridStartDate.AddDays(i);
            var isCurrentMonth = date.Year == year && date.Month == month;
            var isToday = date == today;

            dailyTotals.TryGetValue(date, out var dailyTotal);
            recurringByDate.TryGetValue(date, out var recurringInstances);
            recurringTransfersByDate.TryGetValue(date, out var recurringTransferInstances);

            var actualAmount = dailyTotal?.Total.Amount ?? 0m;
            var projectedAmount = (recurringInstances?.Sum(r => r.Amount.Amount) ?? 0m)
                + (recurringTransferInstances?.Sum(r => r.Amount.Amount) ?? 0m);
            var recurringCount = (recurringInstances?.Count ?? 0) + (recurringTransferInstances?.Count ?? 0);

            days.Add(new CalendarDaySummaryDto
            {
                Date = date,
                IsCurrentMonth = isCurrentMonth,
                IsToday = isToday,
                ActualTotal = new MoneyDto { Currency = "USD", Amount = actualAmount },
                ProjectedTotal = new MoneyDto { Currency = "USD", Amount = projectedAmount },
                CombinedTotal = new MoneyDto { Currency = "USD", Amount = actualAmount + projectedAmount },
                TransactionCount = dailyTotal?.TransactionCount ?? 0,
                RecurringCount = recurringCount,
                HasRecurring = recurringCount > 0,
            });
        }

        // Calculate starting balance and running balances
        var startingBalance = await _balanceCalculationService.GetBalanceBeforeDateAsync(
            gridStartDate,
            accountId,
            cancellationToken);

        var runningBalance = startingBalance.Amount;
        foreach (var day in days)
        {
            runningBalance += day.CombinedTotal.Amount;
            day.EndOfDayBalance = new MoneyDto { Currency = "USD", Amount = runningBalance };
            day.IsBalanceNegative = runningBalance < 0;
        }

        // Calculate month summary (only for current month days)
        var monthSummary = CalculateMonthSummary(days, recurringByDate, year, month);

        return new CalendarGridDto
        {
            Year = year,
            Month = month,
            Days = days,
            MonthSummary = monthSummary,
            StartingBalance = new MoneyDto { Currency = startingBalance.Currency, Amount = startingBalance.Amount },
        };
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
                Category = txn.Category,
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
                    Category = instance.Category,
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
                    Category = null,
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
                Amount = DomainToDtoMapper.ToDto(txn.Amount),
                Category = txn.Category,
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
                            Category = instance.Category,
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

            // Add recurring transfer instances
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
                            Category = null,
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
        var dailyBalances = new List<DailyBalanceSummaryDto>();
        var dayBalance = startingBalance.Amount;

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

    private async Task<IReadOnlyList<RecurringTransaction>> GetRecurringTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        return accountId.HasValue
            ? await _recurringRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringRepository.GetActiveAsync(cancellationToken);
    }

    private static CalendarMonthSummaryDto CalculateMonthSummary(
        List<CalendarDaySummaryDto> days,
        Dictionary<DateOnly, List<Domain.RecurringInstanceInfo>> recurringByDate,
        int year,
        int month)
    {
        var currentMonthDays = days.Where(d => d.IsCurrentMonth).ToList();

        var totalIncome = currentMonthDays
            .Where(d => d.ActualTotal.Amount > 0)
            .Sum(d => d.ActualTotal.Amount);

        var totalExpenses = currentMonthDays
            .Where(d => d.ActualTotal.Amount < 0)
            .Sum(d => d.ActualTotal.Amount);

        var projectedIncome = currentMonthDays
            .Where(d => d.ProjectedTotal.Amount > 0)
            .Sum(d => d.ProjectedTotal.Amount);

        var projectedExpenses = currentMonthDays
            .Where(d => d.ProjectedTotal.Amount < 0)
            .Sum(d => d.ProjectedTotal.Amount);

        return new CalendarMonthSummaryDto
        {
            TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
            TotalExpenses = new MoneyDto { Currency = "USD", Amount = totalExpenses },
            NetChange = new MoneyDto { Currency = "USD", Amount = totalIncome + totalExpenses },
            ProjectedIncome = new MoneyDto { Currency = "USD", Amount = projectedIncome },
            ProjectedExpenses = new MoneyDto { Currency = "USD", Amount = projectedExpenses },
        };
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

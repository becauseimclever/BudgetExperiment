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
    private const int DaysInWeek = 7;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarGridService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    public CalendarGridService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _accountRepository = accountRepository;
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

        // Fetch data in parallel
        var dailyTotalsTask = _transactionRepository.GetDailyTotalsAsync(year, month, accountId, cancellationToken);
        var recurringTask = GetRecurringTransactionsAsync(accountId, cancellationToken);

        await Task.WhenAll(dailyTotalsTask, recurringTask);

        var dailyTotals = dailyTotalsTask.Result.ToDictionary(d => d.Date);
        var recurringTransactions = recurringTask.Result;

        // Project recurring instances for the grid date range
        var recurringByDate = await GetRecurringInstancesByDateAsync(
            recurringTransactions,
            gridStartDate,
            gridEndDate,
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

            var actualAmount = dailyTotal?.Total.Amount ?? 0m;
            var projectedAmount = recurringInstances?.Sum(r => r.Amount.Amount) ?? 0m;

            days.Add(new CalendarDaySummaryDto
            {
                Date = date,
                IsCurrentMonth = isCurrentMonth,
                IsToday = isToday,
                ActualTotal = new MoneyDto { Currency = "USD", Amount = actualAmount },
                ProjectedTotal = new MoneyDto { Currency = "USD", Amount = projectedAmount },
                CombinedTotal = new MoneyDto { Currency = "USD", Amount = actualAmount + projectedAmount },
                TransactionCount = dailyTotal?.TransactionCount ?? 0,
                RecurringCount = recurringInstances?.Count ?? 0,
                HasRecurring = recurringInstances?.Count > 0,
            });
        }

        // Calculate month summary (only for current month days)
        var monthSummary = CalculateMonthSummary(days, recurringByDate, year, month);

        return new CalendarGridDto
        {
            Year = year,
            Month = month,
            Days = days,
            MonthSummary = monthSummary,
        };
    }

    /// <inheritdoc/>
    public async Task<DayDetailDto> GetDayDetailAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        // Fetch data in parallel
        var transactionsTask = _transactionRepository.GetByDateRangeAsync(date, date, accountId, cancellationToken);
        var accountsTask = _accountRepository.GetAllAsync(cancellationToken);
        var recurringTask = GetRecurringTransactionsAsync(accountId, cancellationToken);

        await Task.WhenAll(transactionsTask, accountsTask, recurringTask);

        var transactions = transactionsTask.Result;
        var accountMap = accountsTask.Result.ToDictionary(a => a.Id, a => a.Name);
        var recurringTransactions = recurringTask.Result;

        // Get recurring instances for this specific date
        var recurringInstances = await GetRecurringInstancesForDateAsync(
            recurringTransactions,
            date,
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

        // Calculate summary
        var actualTotal = items.Where(i => i.Type == "transaction").Sum(i => i.Amount.Amount);
        var projectedTotal = items.Where(i => i.Type == "recurring").Sum(i => i.Amount.Amount);

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

    private async Task<Dictionary<DateOnly, List<RecurringInstanceInfo>>> GetRecurringInstancesByDateAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
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

                var instance = new RecurringInstanceInfo
                {
                    RecurringTransactionId = recurring.Id,
                    AccountId = recurring.AccountId,
                    AccountName = accountMap.GetValueOrDefault(recurring.AccountId, string.Empty),
                    Description = exception?.ModifiedDescription ?? recurring.Description,
                    Amount = exception?.ModifiedAmount ?? recurring.Amount,
                    Category = null,
                    IsModified = exception?.ExceptionType == ExceptionType.Modified,
                    IsSkipped = false,
                };

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

    private async Task<List<RecurringInstanceInfo>> GetRecurringInstancesForDateAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly date,
        CancellationToken cancellationToken)
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

            result.Add(new RecurringInstanceInfo
            {
                RecurringTransactionId = recurring.Id,
                AccountId = recurring.AccountId,
                AccountName = accountMap.GetValueOrDefault(recurring.AccountId, string.Empty),
                Description = exception?.ModifiedDescription ?? recurring.Description,
                Amount = exception?.ModifiedAmount ?? recurring.Amount,
                Category = null,
                IsModified = exception?.ExceptionType == ExceptionType.Modified,
                IsSkipped = exception?.ExceptionType == ExceptionType.Skipped,
            });
        }

        return result;
    }

    private static CalendarMonthSummaryDto CalculateMonthSummary(
        List<CalendarDaySummaryDto> days,
        Dictionary<DateOnly, List<RecurringInstanceInfo>> recurringByDate,
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

    /// <summary>
    /// Internal class to hold recurring instance information during processing.
    /// </summary>
    private sealed class RecurringInstanceInfo
    {
        public Guid RecurringTransactionId { get; set; }

        public Guid AccountId { get; set; }

        public string AccountName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public MoneyValue Amount { get; set; } = null!;

        public string? Category { get; set; }

        public bool IsModified { get; set; }

        public bool IsSkipped { get; set; }
    }
}

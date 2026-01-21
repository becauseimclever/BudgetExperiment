// <copyright file="CalendarGridService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Service for building complete calendar grid views with all data pre-computed.
/// </summary>
public sealed class CalendarGridService : ICalendarGridService
{
    private const int GridDays = 42; // 6 weeks * 7 days

    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringTransferRepository _recurringTransferRepository;
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
    /// <param name="balanceCalculationService">The balance calculation service.</param>
    /// <param name="recurringInstanceProjector">The recurring instance projector.</param>
    /// <param name="recurringTransferInstanceProjector">The recurring transfer instance projector.</param>
    /// <param name="autoRealizeService">The auto-realize service.</param>
    public CalendarGridService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IBalanceCalculationService balanceCalculationService,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector,
        IAutoRealizeService autoRealizeService)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
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
        var days = BuildGridDays(gridStartDate, year, month, today, dailyTotals, recurringByDate, recurringTransfersByDate);

        // Calculate starting balance and running balances
        var startingBalance = await _balanceCalculationService.GetBalanceBeforeDateAsync(
            gridStartDate,
            accountId,
            cancellationToken);

        CalculateRunningBalances(days, startingBalance.Amount);

        // Calculate month summary (only for current month days)
        var monthSummary = CalculateMonthSummary(days);

        return new CalendarGridDto
        {
            Year = year,
            Month = month,
            Days = days,
            MonthSummary = monthSummary,
            StartingBalance = new MoneyDto { Currency = startingBalance.Currency, Amount = startingBalance.Amount },
        };
    }

    private static List<CalendarDaySummaryDto> BuildGridDays(
        DateOnly gridStartDate,
        int year,
        int month,
        DateOnly today,
        Dictionary<DateOnly, DailyTotal> dailyTotals,
        Dictionary<DateOnly, List<RecurringInstanceInfo>> recurringByDate,
        Dictionary<DateOnly, List<RecurringTransferInstanceInfo>> recurringTransfersByDate)
    {
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

        return days;
    }

    private static void CalculateRunningBalances(List<CalendarDaySummaryDto> days, decimal startingBalance)
    {
        var runningBalance = startingBalance;
        foreach (var day in days)
        {
            runningBalance += day.CombinedTotal.Amount;
            day.EndOfDayBalance = new MoneyDto { Currency = "USD", Amount = runningBalance };
            day.IsBalanceNegative = runningBalance < 0;
        }
    }

    private static CalendarMonthSummaryDto CalculateMonthSummary(List<CalendarDaySummaryDto> days)
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

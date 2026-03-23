// <copyright file="CalendarGridService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Microsoft.Extensions.DependencyInjection;

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
    private readonly ICurrencyProvider _currencyProvider;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IUserContext? _userContext;

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
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="scopeFactory">The scope factory for parallel query scopes.</param>
    /// <param name="userContext">The current user context.</param>
    public CalendarGridService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IBalanceCalculationService balanceCalculationService,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector,
        IAutoRealizeService autoRealizeService,
        ICurrencyProvider currencyProvider,
        IServiceScopeFactory? scopeFactory = null,
        IUserContext? userContext = null)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _balanceCalculationService = balanceCalculationService;
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
        _autoRealizeService = autoRealizeService;
        _currencyProvider = currencyProvider;
        _scopeFactory = scopeFactory;
        _userContext = userContext;
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

        var dailyTotalsTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ITransactionRepository>()
                .GetDailyTotalsAsync(year, month, accountId, ct),
            cancellationToken);
        var recurringTransactionsTask = RunInNewScopeAsync(
            (sp, ct) =>
            {
                var repo = sp.GetRequiredService<IRecurringTransactionRepository>();
                return accountId.HasValue
                    ? repo.GetByAccountIdAsync(accountId.Value, ct)
                    : repo.GetActiveAsync(ct);
            },
            cancellationToken);
        var recurringTransfersTask = RunInNewScopeAsync(
            (sp, ct) =>
            {
                var repo = sp.GetRequiredService<IRecurringTransferRepository>();
                return accountId.HasValue
                    ? repo.GetByAccountIdAsync(accountId.Value, ct)
                    : repo.GetActiveAsync(ct);
            },
            cancellationToken);
        var currencyTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ICurrencyProvider>().GetCurrencyAsync(ct),
            cancellationToken);
        var startingBalanceTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<IBalanceCalculationService>()
                .GetOpeningBalanceForDateAsync(gridStartDate, accountId, ct),
            cancellationToken);
        var initialBalancesTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<IBalanceCalculationService>()
                .GetInitialBalancesByDateRangeAsync(gridStartDate, gridEndDate, accountId, ct),
            cancellationToken);

        await Task.WhenAll(
            dailyTotalsTask,
            recurringTransactionsTask,
            recurringTransfersTask,
            currencyTask,
            startingBalanceTask,
            initialBalancesTask);

        var dailyTotalsList = await dailyTotalsTask;
        var dailyTotals = dailyTotalsList.ToDictionary(d => d.Date);
        var recurringTransactions = await recurringTransactionsTask;
        var recurringTransfers = await recurringTransfersTask;

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
        var currency = await currencyTask;
        var days = BuildGridDays(gridStartDate, year, month, today, dailyTotals, recurringByDate, recurringTransfersByDate, currency);

        // Calculate starting balance (opening balance for grid start date)
        // This includes initial balances for accounts starting BEFORE grid start,
        // plus transactions before grid start (but not on grid start itself)
        var startingBalance = await startingBalanceTask;

        // Get initial balances for accounts that start WITHIN the grid date range
        // These need to be added to the running balance on their respective start dates
        var initialBalancesInGrid = await initialBalancesTask;

        CalculateRunningBalances(days, startingBalance.Amount, initialBalancesInGrid, currency);

        // Calculate month summary (only for current month days)
        var monthSummary = CalculateMonthSummary(days, currency);

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
        Dictionary<DateOnly, DailyTotalValue> dailyTotals,
        Dictionary<DateOnly, List<RecurringInstanceInfoValue>> recurringByDate,
        Dictionary<DateOnly, List<RecurringTransferInstanceInfoValue>> recurringTransfersByDate,
        string currency)
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
                ActualTotal = new MoneyDto { Currency = currency, Amount = actualAmount },
                ProjectedTotal = new MoneyDto { Currency = currency, Amount = projectedAmount },
                CombinedTotal = new MoneyDto { Currency = currency, Amount = actualAmount + projectedAmount },
                TransactionCount = dailyTotal?.TransactionCount ?? 0,
                RecurringCount = recurringCount,
                HasRecurring = recurringCount > 0,
            });
        }

        return days;
    }

    private static void CalculateRunningBalances(
        List<CalendarDaySummaryDto> days,
        decimal startingBalance,
        Dictionary<DateOnly, decimal> initialBalancesInGrid,
        string currency)
    {
        var runningBalance = startingBalance;
        foreach (var day in days)
        {
            // Add any initial balances for accounts starting on this day
            if (initialBalancesInGrid.TryGetValue(day.Date, out var initialBalanceOnDay))
            {
                runningBalance += initialBalanceOnDay;
            }

            runningBalance += day.CombinedTotal.Amount;
            day.EndOfDayBalance = new MoneyDto { Currency = currency, Amount = runningBalance };
            day.IsBalanceNegative = runningBalance < 0;
        }
    }

    private static CalendarMonthSummaryDto CalculateMonthSummary(List<CalendarDaySummaryDto> days, string currency)
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
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            TotalExpenses = new MoneyDto { Currency = currency, Amount = totalExpenses },
            NetChange = new MoneyDto { Currency = currency, Amount = totalIncome + totalExpenses },
            ProjectedIncome = new MoneyDto { Currency = currency, Amount = projectedIncome },
            ProjectedExpenses = new MoneyDto { Currency = currency, Amount = projectedExpenses },
        };
    }

    private async Task<T> RunInNewScopeAsync<T>(
        Func<IServiceProvider, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (_scopeFactory is null || _userContext is null)
        {
            var provider = new FallbackServiceProvider(
                _transactionRepository,
                _recurringRepository,
                _recurringTransferRepository,
                _balanceCalculationService,
                _currencyProvider);
            var fallbackTask = action(provider, cancellationToken);
            return fallbackTask is null
                ? default!
                : await fallbackTask;
        }

        using var scope = _scopeFactory.CreateScope();
        var scopedUserContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        scopedUserContext.SetScope(_userContext.CurrentScope);
        var scopedTask = action(scope.ServiceProvider, cancellationToken);
        return scopedTask is null
            ? default!
            : await scopedTask;
    }

    private sealed class FallbackServiceProvider : IServiceProvider
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRecurringTransactionRepository _recurringRepository;
        private readonly IRecurringTransferRepository _recurringTransferRepository;
        private readonly IBalanceCalculationService _balanceCalculationService;
        private readonly ICurrencyProvider _currencyProvider;

        public FallbackServiceProvider(
            ITransactionRepository transactionRepository,
            IRecurringTransactionRepository recurringRepository,
            IRecurringTransferRepository recurringTransferRepository,
            IBalanceCalculationService balanceCalculationService,
            ICurrencyProvider currencyProvider)
        {
            _transactionRepository = transactionRepository;
            _recurringRepository = recurringRepository;
            _recurringTransferRepository = recurringTransferRepository;
            _balanceCalculationService = balanceCalculationService;
            _currencyProvider = currencyProvider;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ITransactionRepository))
            {
                return _transactionRepository;
            }

            if (serviceType == typeof(IRecurringTransactionRepository))
            {
                return _recurringRepository;
            }

            if (serviceType == typeof(IRecurringTransferRepository))
            {
                return _recurringTransferRepository;
            }

            if (serviceType == typeof(IBalanceCalculationService))
            {
                return _balanceCalculationService;
            }

            if (serviceType == typeof(ICurrencyProvider))
            {
                return _currencyProvider;
            }

            return null;
        }
    }
}

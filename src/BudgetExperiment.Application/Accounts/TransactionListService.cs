// <copyright file="TransactionListService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Microsoft.Extensions.DependencyInjection;

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
    private readonly ICurrencyProvider _currencyProvider;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IUserContext? _userContext;

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
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="scopeFactory">The scope factory for parallel query scopes.</param>
    /// <param name="userContext">The current user context.</param>
    public TransactionListService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
        IBalanceCalculationService balanceCalculationService,
        IRecurringInstanceProjector recurringInstanceProjector,
        IRecurringTransferInstanceProjector recurringTransferInstanceProjector,
        ICurrencyProvider currencyProvider,
        IServiceScopeFactory? scopeFactory = null,
        IUserContext? userContext = null)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
        _balanceCalculationService = balanceCalculationService;
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
        _currencyProvider = currencyProvider;
        _scopeFactory = scopeFactory;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true,
        CancellationToken cancellationToken = default)
    {
        var accountTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<IAccountRepository>()
                .GetByIdAsync(accountId, ct),
            cancellationToken);
        var currencyTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ICurrencyProvider>().GetCurrencyAsync(ct),
            cancellationToken);
        var transactionsTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ITransactionRepository>()
                .GetByDateRangeAsync(startDate, endDate, accountId, ct),
            cancellationToken);
        var startingBalanceTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<IBalanceCalculationService>()
                .GetBalanceBeforeDateAsync(startDate, accountId, ct),
            cancellationToken);

        var recurringTransactionsTask = Task.FromResult<IReadOnlyList<RecurringTransaction>>(Array.Empty<RecurringTransaction>());
        var recurringTransfersTask = Task.FromResult<IReadOnlyList<RecurringTransfer>>(Array.Empty<RecurringTransfer>());

        if (includeRecurring)
        {
            recurringTransactionsTask = RunInNewScopeAsync(
                (sp, ct) => sp.GetRequiredService<IRecurringTransactionRepository>()
                    .GetByAccountIdAsync(accountId, ct),
                cancellationToken);
            recurringTransfersTask = RunInNewScopeAsync(
                (sp, ct) => sp.GetRequiredService<IRecurringTransferRepository>()
                    .GetByAccountIdAsync(accountId, ct),
                cancellationToken);
        }

        await Task.WhenAll(
            accountTask,
            currencyTask,
            transactionsTask,
            startingBalanceTask,
            recurringTransactionsTask,
            recurringTransfersTask);

        var account = await accountTask
            ?? throw new InvalidOperationException($"Account with ID {accountId} not found.");
        var currency = await currencyTask;
        var transactions = await transactionsTask;
        var startingBalance = await startingBalanceTask;
        var recurringTransactions = await recurringTransactionsTask;
        var recurringTransfers = await recurringTransfersTask;

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
                CreatedAtUtc = txn.CreatedAtUtc,
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
            await AddRecurringTransactionInstancesAsync(
                accountId,
                startDate,
                endDate,
                transactions,
                recurringTransactions,
                items,
                cancellationToken);
            await AddRecurringTransferInstancesAsync(
                accountId,
                startDate,
                endDate,
                transactions,
                recurringTransfers,
                items,
                cancellationToken);
        }

        // Sort items by date descending, then by created timestamp
        var sortedItems = items.OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAtUtc ?? DateTime.MinValue).ToList();

        // GetBalanceBeforeDateAsync returns the balance *strictly before* startDate.
        // When startDate <= InitialBalanceDate the initial balance is not included
        // in the "before" result, so we must add it to the running balance seed.
        var balanceSeed = startingBalance.Amount;
        if (startDate <= account.InitialBalanceDate)
        {
            balanceSeed += account.InitialBalance.Amount;
        }

        // Sort items by date ascending for running balance calculation
        var sortedForBalance = items.OrderBy(i => i.Date).ThenBy(i => i.CreatedAtUtc ?? DateTime.MinValue).ToList();

        // Calculate running balance for each item
        var runningBalance = balanceSeed;
        foreach (var item in sortedForBalance)
        {
            runningBalance += item.Amount.Amount;
            item.RunningBalance = new MoneyDto { Currency = currency, Amount = runningBalance };
        }

        // Calculate daily balance summaries
        var dailyBalances = CalculateDailyBalances(sortedForBalance, balanceSeed, currency);

        // Calculate summary
        var transactionCount = sortedItems.Count(i => i.Type == "transaction");
        var recurringCount = sortedItems.Count(i => i.Type == "recurring" || i.Type == "recurring-transfer");
        var totalAmount = sortedItems.Sum(i => i.Amount.Amount);
        var totalIncome = sortedItems.Where(i => i.Amount.Amount > 0).Sum(i => i.Amount.Amount);
        var totalExpenses = sortedItems.Where(i => i.Amount.Amount < 0).Sum(i => i.Amount.Amount);
        var currentBalance = runningBalance;

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
                TotalAmount = new MoneyDto { Currency = currency, Amount = totalAmount },
                TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
                TotalExpenses = new MoneyDto { Currency = currency, Amount = totalExpenses },
                TransactionCount = transactionCount,
                RecurringCount = recurringCount,
                CurrentBalance = new MoneyDto { Currency = currency, Amount = currentBalance },
            },
            DailyBalances = dailyBalances.OrderByDescending(d => d.Date).ToList(),
            StartingBalance = new MoneyDto { Currency = startingBalance.Currency, Amount = balanceSeed },
        };
    }

    private static List<DailyBalanceSummaryDto> CalculateDailyBalances(
        List<TransactionListItemDto> sortedForBalance,
        decimal startingBalanceAmount,
        string currency)
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
                StartingBalance = new MoneyDto { Currency = currency, Amount = dayStart },
                EndingBalance = new MoneyDto { Currency = currency, Amount = dayBalance },
                DayTotal = new MoneyDto { Currency = currency, Amount = dayTotal },
                TransactionCount = dayGroup.Count(),
            });
        }

        return dailyBalances;
    }

    private static bool IsRealizedAsTransaction(IReadOnlyList<Transaction> transactions, Guid recurringTransactionId, DateOnly date)
        => transactions.Any(t => t.RecurringTransactionId == recurringTransactionId && t.Date == date);

    private static bool IsRealizedAsTransfer(IReadOnlyList<Transaction> transactions, Guid recurringTransferId, DateOnly date)
        => transactions.Any(t => t.RecurringTransferId == recurringTransferId && t.Date == date);

    private static TransactionListItemDto BuildRecurringTransactionItem(RecurringInstanceInfoValue instance, DateOnly date)
        => new()
        {
            Id = instance.RecurringTransactionId,
            Type = "recurring",
            Date = date,
            Description = instance.Description,
            Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
            CategoryId = instance.CategoryId,
            CategoryName = instance.CategoryName,
            CreatedAtUtc = null,
            IsModified = instance.IsModified,
            RecurringTransactionId = instance.RecurringTransactionId,
            RecurringTransferId = null,
            IsTransfer = false,
            TransferId = null,
            TransferDirection = null,
        };

    private static TransactionListItemDto BuildRecurringTransferItem(RecurringTransferInstanceInfoValue instance, DateOnly date)
        => new()
        {
            Id = instance.RecurringTransferId,
            Type = "recurring-transfer",
            Date = date,
            Description = instance.Description,
            Amount = new MoneyDto { Currency = instance.Amount.Currency, Amount = instance.Amount.Amount },
            CategoryId = null,
            CreatedAtUtc = null,
            IsModified = instance.IsModified,
            RecurringTransactionId = null,
            RecurringTransferId = instance.RecurringTransferId,
            IsTransfer = true,
            TransferId = null,
            TransferDirection = instance.TransferDirection,
        };

    private async Task AddRecurringTransactionInstancesAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        List<TransactionListItemDto> items,
        CancellationToken cancellationToken)
    {
        var recurringInstances = await _recurringInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            startDate,
            endDate,
            cancellationToken);

        foreach (var (date, instances) in recurringInstances)
        {
            items.AddRange(instances
                .Where(i => i.AccountId == accountId)
                .Where(i => !IsRealizedAsTransaction(transactions, i.RecurringTransactionId, date))
                .Select(i => BuildRecurringTransactionItem(i, date)));
        }
    }

    private async Task AddRecurringTransferInstancesAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        List<TransactionListItemDto> items,
        CancellationToken cancellationToken)
    {
        var recurringTransferInstances = await _recurringTransferInstanceProjector.GetInstancesByDateRangeAsync(
            recurringTransfers,
            startDate,
            endDate,
            accountId,
            cancellationToken);

        foreach (var (date, instances) in recurringTransferInstances)
        {
            items.AddRange(instances
                .Where(i => !IsRealizedAsTransfer(transactions, i.RecurringTransferId, date))
                .Select(i => BuildRecurringTransferItem(i, date)));
        }
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
                _accountRepository,
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
        private readonly IAccountRepository _accountRepository;
        private readonly IBalanceCalculationService _balanceCalculationService;
        private readonly ICurrencyProvider _currencyProvider;

        public FallbackServiceProvider(
            ITransactionRepository transactionRepository,
            IRecurringTransactionRepository recurringRepository,
            IRecurringTransferRepository recurringTransferRepository,
            IAccountRepository accountRepository,
            IBalanceCalculationService balanceCalculationService,
            ICurrencyProvider currencyProvider)
        {
            _transactionRepository = transactionRepository;
            _recurringRepository = recurringRepository;
            _recurringTransferRepository = recurringTransferRepository;
            _accountRepository = accountRepository;
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

            if (serviceType == typeof(IAccountRepository))
            {
                return _accountRepository;
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

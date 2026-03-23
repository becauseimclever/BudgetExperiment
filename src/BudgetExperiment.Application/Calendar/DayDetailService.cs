// <copyright file="DayDetailService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Microsoft.Extensions.DependencyInjection;

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
    private readonly ICurrencyProvider _currencyProvider;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IUserContext? _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DayDetailService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="recurringInstanceProjector">The recurring instance projector.</param>
    /// <param name="recurringTransferInstanceProjector">The recurring transfer instance projector.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="scopeFactory">The scope factory for parallel query scopes.</param>
    /// <param name="userContext">The current user context.</param>
    public DayDetailService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
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
        _recurringInstanceProjector = recurringInstanceProjector;
        _recurringTransferInstanceProjector = recurringTransferInstanceProjector;
        _currencyProvider = currencyProvider;
        _scopeFactory = scopeFactory;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<DayDetailDto> GetDayDetailAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var currencyTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ICurrencyProvider>().GetCurrencyAsync(ct),
            cancellationToken);
        var transactionsTask = RunInNewScopeAsync(
            (sp, ct) => sp.GetRequiredService<ITransactionRepository>()
                .GetByDateRangeAsync(date, date, accountId, ct),
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

        await Task.WhenAll(
            currencyTask,
            transactionsTask,
            recurringTransactionsTask,
            recurringTransfersTask);

        var currency = await currencyTask;
        var transactions = await transactionsTask;
        var recurringTransactions = await recurringTransactionsTask;
        var recurringTransfers = await recurringTransfersTask;

        var accountIds = transactions.Select(t => t.AccountId).Distinct().ToList();
        var accountMap = await RunInNewScopeAsync(
                (sp, ct) => sp.GetRequiredService<IAccountRepository>()
                    .GetAccountNamesByIdsAsync(accountIds, ct),
                cancellationToken)
            ?? new Dictionary<Guid, string>();

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
                Amount = CommonMapper.ToDto(txn.Amount),
                CategoryId = txn.CategoryId,
                AccountName = accountMap.GetValueOrDefault(txn.AccountId, string.Empty),
                AccountId = txn.AccountId,
                CreatedAtUtc = txn.CreatedAtUtc,
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
                    CreatedAtUtc = null,
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
                    CreatedAtUtc = null,
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
            Items = items.OrderBy(i => i.Type).ThenBy(i => i.CreatedAtUtc ?? DateTime.MaxValue).ToList(),
            Summary = new DayDetailSummaryDto
            {
                TotalActual = new MoneyDto { Currency = currency, Amount = actualTotal },
                TotalProjected = new MoneyDto { Currency = currency, Amount = projectedTotal },
                CombinedTotal = new MoneyDto { Currency = currency, Amount = actualTotal + projectedTotal },
                ItemCount = items.Count,
            },
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
                _accountRepository,
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
        private readonly ICurrencyProvider _currencyProvider;

        public FallbackServiceProvider(
            ITransactionRepository transactionRepository,
            IRecurringTransactionRepository recurringRepository,
            IRecurringTransferRepository recurringTransferRepository,
            IAccountRepository accountRepository,
            ICurrencyProvider currencyProvider)
        {
            _transactionRepository = transactionRepository;
            _recurringRepository = recurringRepository;
            _recurringTransferRepository = recurringTransferRepository;
            _accountRepository = accountRepository;
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

            if (serviceType == typeof(ICurrencyProvider))
            {
                return _currencyProvider;
            }

            return null;
        }
    }
}

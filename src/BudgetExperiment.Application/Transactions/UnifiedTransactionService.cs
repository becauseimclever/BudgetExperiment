// <copyright file="UnifiedTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Common;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Repositories;

namespace BudgetExperiment.Application.Transactions;

/// <summary>
/// Service for the unified transaction list: paginated, filtered, sorted view of all transactions.
/// </summary>
public sealed class UnifiedTransactionService : IUnifiedTransactionService
{
    private const int MaxPageSize = 100;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedTransactionService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    public UnifiedTransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
    }

    /// <inheritdoc />
    public async Task<UnifiedTransactionPageDto> GetPagedAsync(
        UnifiedTransactionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        KakeiboCategory? kakeiboFilter = null;
        if (!string.IsNullOrWhiteSpace(filter.KakeiboCategory) &&
            Enum.TryParse<KakeiboCategory>(filter.KakeiboCategory, ignoreCase: true, out var parsedKakeibo))
        {
            kakeiboFilter = parsedKakeibo;
        }

        var (items, totalCount) = await _transactionRepository.GetUnifiedPagedAsync(
            filter.AccountId,
            filter.CategoryId,
            filter.Uncategorized,
            filter.StartDate,
            filter.EndDate,
            filter.Description,
            filter.MinAmount,
            filter.MaxAmount,
            filter.SortBy,
            filter.SortDescending,
            skip,
            pageSize,
            kakeiboFilter,
            cancellationToken);

        // Build account name lookup
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountLookup = accounts.ToDictionary(a => a.Id, a => a.Name);

        // Map items
        var mappedItems = items.Select(t => new UnifiedTransactionItemDto
        {
            Id = t.Id,
            Date = t.Date,
            Description = t.Description,
            Amount = CommonMapper.ToDto(t.Amount),
            AccountId = t.AccountId,
            AccountName = accountLookup.GetValueOrDefault(t.AccountId, string.Empty),
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            IsRecurring = t.RecurringTransactionId.HasValue,
            IsTransfer = t.IsTransfer,
            EffectiveKakeiboCategory = (t.KakeiboOverride ?? t.Category?.KakeiboCategory)?.ToString(),
            IsKakeiboOverride = t.KakeiboOverride.HasValue,
        }).ToList();

        // Compute summary from the current page items
        var summary = BuildSummary(items);

        // Build balance info and running balances if filtered to a single account
        AccountBalanceInfoDto? balanceInfo = null;
        Dictionary<Guid, decimal>? runningBalances = null;
        if (filter.AccountId.HasValue)
        {
            (balanceInfo, runningBalances) = await this.BuildBalanceDataAsync(
                filter.AccountId.Value,
                cancellationToken);
        }

        // Apply running balances to mapped items
        if (runningBalances != null)
        {
            var currency = balanceInfo!.CurrentBalance.Currency;
            foreach (var item in mappedItems)
            {
                if (runningBalances.TryGetValue(item.Id, out var balance))
                {
                    item.RunningBalance = new MoneyDto
                    {
                        Currency = currency,
                        Amount = balance,
                    };
                }
            }
        }

        return new UnifiedTransactionPageDto
        {
            Items = mappedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Summary = summary,
            BalanceInfo = balanceInfo,
        };
    }

    private static UnifiedTransactionSummaryDto BuildSummary(IReadOnlyList<Transaction> items)
    {
        var currency = items.Count > 0 ? items[0].Amount.Currency : "USD";
        var totalAmount = items.Sum(t => t.Amount.Amount);
        var incomeTotal = items.Where(t => t.Amount.Amount > 0).Sum(t => t.Amount.Amount);
        var expenseTotal = items.Where(t => t.Amount.Amount < 0).Sum(t => t.Amount.Amount);
        var uncategorizedCount = items.Count(t => t.CategoryId == null);

        return new UnifiedTransactionSummaryDto
        {
            TotalCount = items.Count,
            TotalAmount = new MoneyDto { Currency = currency, Amount = totalAmount },
            IncomeTotal = new MoneyDto { Currency = currency, Amount = incomeTotal },
            ExpenseTotal = new MoneyDto { Currency = currency, Amount = expenseTotal },
            UncategorizedCount = uncategorizedCount,
        };
    }

    private async Task<(AccountBalanceInfoDto? BalanceInfo, Dictionary<Guid, decimal>? RunningBalances)> BuildBalanceDataAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return (null, null);
        }

        // Get all transactions for the account to compute current balance and running balances
        var allTransactions = await _transactionRepository.GetByDateRangeAsync(
            DateOnly.MinValue,
            DateOnly.MaxValue,
            accountId,
            cancellationToken);

        var transactionTotal = allTransactions.Sum(t => t.Amount.Amount);
        var currentBalance = account.InitialBalance.Amount + transactionTotal;

        var balanceInfo = new AccountBalanceInfoDto
        {
            InitialBalance = CommonMapper.ToDto(account.InitialBalance),
            InitialBalanceDate = account.InitialBalanceDate,
            CurrentBalance = new MoneyDto
            {
                Currency = account.InitialBalance.Currency,
                Amount = currentBalance,
            },
        };

        // Compute running balance for each transaction in chronological order
        var sorted = allTransactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAtUtc)
            .ThenBy(t => t.Id);

        var runningBalances = new Dictionary<Guid, decimal>();
        var runningBalance = account.InitialBalance.Amount;
        foreach (var txn in sorted)
        {
            runningBalance += txn.Amount.Amount;
            runningBalances[txn.Id] = runningBalance;
        }

        return (balanceInfo, runningBalances);
    }
}

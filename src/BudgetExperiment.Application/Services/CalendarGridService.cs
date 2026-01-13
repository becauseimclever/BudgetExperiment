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
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarGridService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="settingsRepository">The app settings repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public CalendarGridService(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository,
        IAppSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
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
        await AutoRealizePastDueItemsIfEnabledAsync(today, accountId, cancellationToken);

        // Fetch data sequentially (DbContext is not thread-safe for concurrent operations)
        var dailyTotalsList = await _transactionRepository.GetDailyTotalsAsync(year, month, accountId, cancellationToken);
        var dailyTotals = dailyTotalsList.ToDictionary(d => d.Date);
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);

        // Project recurring instances for the grid date range
        var recurringByDate = await GetRecurringInstancesByDateAsync(
            recurringTransactions,
            gridStartDate,
            gridEndDate,
            cancellationToken);

        // Project recurring transfer instances for the grid date range
        var recurringTransfersByDate = await GetRecurringTransferInstancesByDateAsync(
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
        // Fetch data sequentially (DbContext is not thread-safe for concurrent operations)
        var transactions = await _transactionRepository.GetByDateRangeAsync(date, date, accountId, cancellationToken);
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);

        // Get recurring instances for this specific date
        var recurringInstances = await GetRecurringInstancesForDateAsync(
            recurringTransactions,
            date,
            cancellationToken);

        // Get recurring transfer instances for this specific date
        var recurringTransferInstances = await GetRecurringTransferInstancesForDateAsync(
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
            var recurringInstances = await GetRecurringInstancesByDateAsync(
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
            var recurringTransferInstances = await GetRecurringTransferInstancesByDateAsync(
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

    /// <summary>
    /// Internal class to hold recurring transfer instance information during processing.
    /// </summary>
    private sealed class RecurringTransferInstanceInfo
    {
        public Guid RecurringTransferId { get; set; }

        public Guid AccountId { get; set; }

        public string AccountName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public MoneyValue Amount { get; set; } = null!;

        public bool IsModified { get; set; }

        public bool IsSkipped { get; set; }

        public string TransferDirection { get; set; } = string.Empty;
    }

    private async Task<IReadOnlyList<RecurringTransfer>> GetRecurringTransfersAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        return accountId.HasValue
            ? await _recurringTransferRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _recurringTransferRepository.GetActiveAsync(cancellationToken);
    }

    private async Task<Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>> GetRecurringTransferInstancesByDateAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            var occurrences = transfer.GetOccurrencesBetween(fromDate, toDate);
            var exceptions = await _recurringTransferRepository.GetExceptionsByDateRangeAsync(
                transfer.Id,
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

                var effectiveAmount = exception?.ModifiedAmount ?? transfer.Amount;
                var effectiveDescription = exception?.ModifiedDescription ?? transfer.Description;
                var isModified = exception?.ExceptionType == ExceptionType.Modified;

                // Add source account entry (outgoing - negative amount)
                if (!accountId.HasValue || accountId.Value == transfer.SourceAccountId)
                {
                    var sourceInstance = new RecurringTransferInstanceInfo
                    {
                        RecurringTransferId = transfer.Id,
                        AccountId = transfer.SourceAccountId,
                        AccountName = accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty),
                        Description = $"Transfer to {accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty)}: {effectiveDescription}",
                        Amount = MoneyValue.Create(effectiveAmount.Currency, -effectiveAmount.Amount),
                        IsModified = isModified,
                        IsSkipped = false,
                        TransferDirection = "Source",
                    };

                    if (!result.TryGetValue(date, out var sourceList))
                    {
                        sourceList = new List<RecurringTransferInstanceInfo>();
                        result[date] = sourceList;
                    }

                    sourceList.Add(sourceInstance);
                }

                // Add destination account entry (incoming - positive amount)
                if (!accountId.HasValue || accountId.Value == transfer.DestinationAccountId)
                {
                    var destInstance = new RecurringTransferInstanceInfo
                    {
                        RecurringTransferId = transfer.Id,
                        AccountId = transfer.DestinationAccountId,
                        AccountName = accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty),
                        Description = $"Transfer from {accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty)}: {effectiveDescription}",
                        Amount = effectiveAmount,
                        IsModified = isModified,
                        IsSkipped = false,
                        TransferDirection = "Destination",
                    };

                    if (!result.TryGetValue(date, out var destList))
                    {
                        destList = new List<RecurringTransferInstanceInfo>();
                        result[date] = destList;
                    }

                    destList.Add(destInstance);
                }
            }
        }

        return result;
    }

    private async Task<List<RecurringTransferInstanceInfo>> GetRecurringTransferInstancesForDateAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly date,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var result = new List<RecurringTransferInstanceInfo>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            var occurrences = transfer.GetOccurrencesBetween(date, date);
            if (!occurrences.Contains(date))
            {
                continue;
            }

            var exception = await _recurringTransferRepository.GetExceptionAsync(transfer.Id, date, cancellationToken);
            var effectiveAmount = exception?.ModifiedAmount ?? transfer.Amount;
            var effectiveDescription = exception?.ModifiedDescription ?? transfer.Description;
            var isModified = exception?.ExceptionType == ExceptionType.Modified;
            var isSkipped = exception?.ExceptionType == ExceptionType.Skipped;

            // Add source account entry (outgoing - negative amount)
            if (!accountId.HasValue || accountId.Value == transfer.SourceAccountId)
            {
                result.Add(new RecurringTransferInstanceInfo
                {
                    RecurringTransferId = transfer.Id,
                    AccountId = transfer.SourceAccountId,
                    AccountName = accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty),
                    Description = $"Transfer to {accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty)}: {effectiveDescription}",
                    Amount = MoneyValue.Create(effectiveAmount.Currency, -effectiveAmount.Amount),
                    IsModified = isModified,
                    IsSkipped = isSkipped,
                    TransferDirection = "Source",
                });
            }

            // Add destination account entry (incoming - positive amount)
            if (!accountId.HasValue || accountId.Value == transfer.DestinationAccountId)
            {
                result.Add(new RecurringTransferInstanceInfo
                {
                    RecurringTransferId = transfer.Id,
                    AccountId = transfer.DestinationAccountId,
                    AccountName = accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty),
                    Description = $"Transfer from {accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty)}: {effectiveDescription}",
                    Amount = effectiveAmount,
                    IsModified = isModified,
                    IsSkipped = isSkipped,
                    TransferDirection = "Destination",
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Auto-realizes past-due recurring items if the setting is enabled.
    /// </summary>
    private async Task AutoRealizePastDueItemsIfEnabledAsync(
        DateOnly today,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (!settings.AutoRealizePastDueItems)
        {
            return;
        }

        var lookbackDate = today.AddDays(-settings.PastDueLookbackDays);
        var yesterday = today.AddDays(-1);

        var realizedCount = 0;

        // Auto-realize recurring transactions
        realizedCount += await AutoRealizeRecurringTransactionsAsync(
            lookbackDate, yesterday, accountId, cancellationToken);

        // Auto-realize recurring transfers
        realizedCount += await AutoRealizeRecurringTransfersAsync(
            lookbackDate, yesterday, accountId, cancellationToken);

        if (realizedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Auto-realizes past-due recurring transactions.
    /// </summary>
    private async Task<int> AutoRealizeRecurringTransactionsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var recurringTransactions = await GetRecurringTransactionsAsync(accountId, cancellationToken);
        var realizedCount = 0;

        foreach (var recurring in recurringTransactions.Where(r => r.IsActive))
        {
            var occurrences = recurring.GetOccurrencesBetween(fromDate, toDate);

            foreach (var date in occurrences)
            {
                // Skip if already realized
                var existing = await _transactionRepository.GetByRecurringInstanceAsync(
                    recurring.Id, date, cancellationToken);
                if (existing != null)
                {
                    continue;
                }

                // Check for exceptions (skipped or modified)
                var exception = await _recurringRepository.GetExceptionAsync(
                    recurring.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Realize the instance
                var amount = exception?.ModifiedAmount ?? recurring.Amount;
                var description = exception?.ModifiedDescription ?? recurring.Description;
                var actualDate = exception?.ModifiedDate ?? date;

                var transaction = Transaction.CreateFromRecurring(
                    recurring.AccountId,
                    amount,
                    actualDate,
                    description,
                    recurring.Id,
                    date);

                await _transactionRepository.AddAsync(transaction, cancellationToken);
                realizedCount++;
            }
        }

        return realizedCount;
    }

    /// <summary>
    /// Auto-realizes past-due recurring transfers.
    /// </summary>
    private async Task<int> AutoRealizeRecurringTransfersAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var recurringTransfers = await GetRecurringTransfersAsync(accountId, cancellationToken);
        var realizedCount = 0;

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            // Skip if filtering by account and this transfer doesn't involve that account
            if (accountId.HasValue &&
                transfer.SourceAccountId != accountId.Value &&
                transfer.DestinationAccountId != accountId.Value)
            {
                continue;
            }

            var occurrences = transfer.GetOccurrencesBetween(fromDate, toDate);

            foreach (var date in occurrences)
            {
                // Skip if already realized
                var existing = await _transactionRepository.GetByRecurringTransferInstanceAsync(
                    transfer.Id, date, cancellationToken);
                if (existing.Count > 0)
                {
                    continue;
                }

                // Check for exceptions (skipped or modified)
                var exception = await _recurringTransferRepository.GetExceptionAsync(
                    transfer.Id, date, cancellationToken);
                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                // Realize the transfer (creates two transactions - source and destination)
                var amount = exception?.ModifiedAmount ?? transfer.Amount;
                var description = exception?.ModifiedDescription ?? transfer.Description;
                var actualDate = exception?.ModifiedDate ?? date;
                var transferId = Guid.NewGuid();

                // Source transaction (negative)
                var sourceTransaction = Transaction.CreateFromRecurringTransfer(
                    transfer.SourceAccountId,
                    MoneyValue.Create(amount.Currency, -amount.Amount),
                    actualDate,
                    description,
                    transferId,
                    TransferDirection.Source,
                    transfer.Id,
                    date);
                await _transactionRepository.AddAsync(sourceTransaction, cancellationToken);
                realizedCount++;

                // Destination transaction (positive)
                var destTransaction = Transaction.CreateFromRecurringTransfer(
                    transfer.DestinationAccountId,
                    amount,
                    actualDate,
                    description,
                    transferId,
                    TransferDirection.Destination,
                    transfer.Id,
                    date);
                await _transactionRepository.AddAsync(destTransaction, cancellationToken);
                realizedCount++;
            }
        }

        return realizedCount;
    }
}

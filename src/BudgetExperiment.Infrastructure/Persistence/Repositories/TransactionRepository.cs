// <copyright file="TransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.DataHealth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITransactionRepository"/>.
/// </summary>
internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ILogger<TransactionRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public TransactionRepository(BudgetDbContext context, IUserContext userContext, ILogger<TransactionRepository> logger)
    {
        _context = context;
        _userContext = userContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .AsNoTrackingWithIdentityResolution()
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        return await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyTotalValue>> GetDailyTotalsAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        // Group by date and calculate totals
        // Note: This assumes all transactions use the same currency (USD)
        // For multi-currency support, this would need to be grouped by currency too
        var dailyTotals = await query
            .GroupBy(t => t.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(t => t.Amount.Amount),
                Currency = g.First().Amount.Currency,
                Count = g.Count(),
            })
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return dailyTotals
            .Select(d => new DailyTotalValue(
                d.Date,
                MoneyValue.Create(d.Currency, d.TotalAmount),
                d.Count))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .Where(t => t.TransferId == transferId)
            .OrderBy(t => t.TransferDirection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByRecurringInstanceAsync(
        Guid recurringTransactionId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(
                t => t.RecurringTransactionId == recurringTransactionId
                    && t.RecurringInstanceDate == instanceDate,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByRecurringTransferInstanceAsync(
        Guid recurringTransferId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(t => t.RecurringTransferId == recurringTransferId
                && t.RecurringTransferInstanceDate == instanceDate)
            .OrderBy(t => t.TransferDirection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(Transaction entity, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(Transaction entity, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<MoneyValue> GetSpendingByCategoryAsync(
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get the category name to match against transactions
        // For now, transactions use a string category. Later phases will add a proper CategoryId FK.
        // Verify the category exists
        var categoryExists = await _context.BudgetCategories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            return MoneyValue.Create(CurrencyDefaults.DefaultCurrency, 0m);
        }

        // Sum all spending (negative amounts represent expenses) with ownership filtering
        var totalSpending = await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.Date >= startDate
                && t.Date <= endDate
                && t.CategoryId == categoryId
                && t.Amount.Amount < 0)
            .SumAsync(t => Math.Abs(t.Amount.Amount), cancellationToken);

        return MoneyValue.Create(CurrencyDefaults.DefaultCurrency, totalSpending);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, decimal>> GetSpendingByCategoriesAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.Date >= startDate
                && t.Date <= endDate
                && t.TransferId == null
                && t.CategoryId != null
                && t.Amount.Amount < 0)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(t => Math.Abs(t.Amount.Amount)),
            })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetUncategorizedAsync(
        int maxCount = 500,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .Where(t => t.CategoryId == null)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUncategorizedDescriptionsAsync(
        int maxCount = 500,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.CategoryId == null)
            .Select(t => t.Description)
            .Distinct()
            .OrderBy(d => d)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetUncategorizedPagedAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? descriptionContains = null,
        Guid? accountId = null,
        string sortBy = "Date",
        bool sortDescending = true,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.CategoryId == null);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        // Amount filters use absolute value comparison (handles both positive and negative amounts)
        if (minAmount.HasValue)
        {
            var min = minAmount.Value;
            query = query.Where(t => (t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount) >= min);
        }

        if (maxAmount.HasValue)
        {
            var max = maxAmount.Value;
            query = query.Where(t => (t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount) <= max);
        }

        if (!string.IsNullOrWhiteSpace(descriptionContains))
        {
            query = query.Where(t => t.Description.ToLower().Contains(descriptionContains.ToLower()));
        }

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        // Get total count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting (absolute value for amounts)
        query = sortBy.ToUpperInvariant() switch
        {
            "AMOUNT" => sortDescending
                ? query.OrderByDescending(t => t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount).ThenByDescending(t => t.Date)
                : query.OrderBy(t => t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount).ThenBy(t => t.Date),
            "DESCRIPTION" => sortDescending
                ? query.OrderByDescending(t => t.Description).ThenByDescending(t => t.Date)
                : query.OrderBy(t => t.Description).ThenBy(t => t.Date),
            _ => sortDescending
                ? query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.Date).ThenBy(t => t.CreatedAtUtc),
        };

        // Apply paging
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllDescriptionsAsync(
        string searchPrefix = "",
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var query = this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Select(t => t.Description)
            .Distinct();

        if (!string.IsNullOrWhiteSpace(searchPrefix))
        {
            query = query.Where(d => d.StartsWith(searchPrefix));
        }

        return await query
            .OrderBy(d => d)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DuplicateDetectionProjection>> GetTransactionProjectionsForDuplicateDetectionAsync(
        CancellationToken cancellationToken = default)
    {
        var projections = await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.TransferId == null)
            .OrderBy(t => t.AccountId)
            .ThenBy(t => t.Date)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.Date,
                Amount = t.Amount.Amount,
                t.Description,
            })
            .ToListAsync(cancellationToken);

        return projections
            .Select(t => new DuplicateDetectionProjection(t.Id, t.AccountId, t.Date, t.Amount, t.Description))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateGapProjection>> GetTransactionDatesForGapAnalysisAsync(
        CancellationToken cancellationToken = default)
    {
        var dates = await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Select(t => new
            {
                t.AccountId,
                t.Date,
            })
            .Distinct()
            .OrderBy(t => t.AccountId)
            .ThenBy(t => t.Date)
            .ToListAsync(cancellationToken);

        return dates
            .Select(t => new DateGapProjection(t.AccountId, t.Date))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutlierProjection>> GetTransactionAmountsForOutlierAnalysisAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Select(t => new OutlierProjection(t.Id, t.Description, t.Amount.Amount))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetForDuplicateDetectionAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.AccountId == accountId
                && t.Date >= startDate
                && t.Date <= endDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByImportBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.ImportBatchId == batchId)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetAllWithLocationAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.Location != null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetClearedByAccountAsync(
        Guid accountId,
        DateOnly? upToDate,
        CancellationToken ct)
    {
        var query = this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.AccountId == accountId && t.IsCleared);

        if (upToDate.HasValue)
        {
            query = query.Where(t => t.Date <= upToDate.Value);
        }

        return await query
            .OrderBy(t => t.Date)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<MoneyValue> GetClearedBalanceSumAsync(
        Guid accountId,
        DateOnly? upToDate,
        CancellationToken ct)
    {
        var query = this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.AccountId == accountId && t.IsCleared);

        if (upToDate.HasValue)
        {
            query = query.Where(t => t.Date <= upToDate.Value);
        }

        var result = await query
            .GroupBy(t => t.Amount.Currency)
            .Select(g => new { Currency = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .FirstOrDefaultAsync(ct);

        return result is null
            ? MoneyValue.Create(CurrencyDefaults.DefaultCurrency, 0m)
            : MoneyValue.Create(result.Currency, result.Total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByReconciliationRecordAsync(
        Guid reconciliationRecordId,
        CancellationToken ct)
    {
        return await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.ReconciliationRecordId == reconciliationRecordId)
            .AsNoTracking()
            .OrderBy(t => t.Date)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetUnifiedPagedAsync(
        Guid? accountId = null,
        Guid? categoryId = null,
        bool? uncategorized = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? descriptionContains = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string sortBy = "date",
        bool sortDescending = true,
        int skip = 0,
        int take = 50,
        KakeiboCategory? kakeiboCategory = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = this.ApplyScopeFilter(_context.Transactions)
            .Include(t => t.Category)
            .AsNoTrackingWithIdentityResolution();

        // Apply filters
        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (uncategorized == true)
        {
            query = query.Where(t => t.CategoryId == null);
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(descriptionContains))
        {
            query = query.Where(t => t.Description.ToLower().Contains(descriptionContains.ToLower()));
        }

        if (minAmount.HasValue)
        {
            var min = minAmount.Value;
            query = query.Where(t => (t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount) >= min);
        }

        if (maxAmount.HasValue)
        {
            var max = maxAmount.Value;
            query = query.Where(t => (t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount) <= max);
        }

        if (kakeiboCategory.HasValue)
        {
            var kc = kakeiboCategory.Value;
            query = query.Where(t =>
                (t.KakeiboOverride != null && t.KakeiboOverride == kc) ||
                (t.KakeiboOverride == null && t.Category != null && t.Category.KakeiboCategory == kc));
        }

        // Get total count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        IOrderedQueryable<Transaction> orderedQuery = sortBy.ToUpperInvariant() switch
        {
            "AMOUNT" => sortDescending
                ? query.OrderByDescending(t => t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount)
                : query.OrderBy(t => t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount),
            "DESCRIPTION" => sortDescending
                ? query.OrderByDescending(t => t.Description)
                : query.OrderBy(t => t.Description),
            "CATEGORY" => sortDescending
                ? query.OrderByDescending(t => t.Category != null ? t.Category.Name : string.Empty)
                : query.OrderBy(t => t.Category != null ? t.Category.Name : string.Empty),
            "ACCOUNT" => sortDescending
                ? query.OrderByDescending(t => _context.Accounts
                    .Where(a => a.Id == t.AccountId)
                    .Select(a => a.Name)
                    .FirstOrDefault())
                : query.OrderBy(t => _context.Accounts
                    .Where(a => a.Id == t.AccountId)
                    .Select(a => a.Name)
                    .FirstOrDefault()),
            _ => sortDescending
                ? query.OrderByDescending(t => t.Date)
                : query.OrderBy(t => t.Date),
        };

        // Secondary sort for stability
        var items = await orderedQuery
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task DeleteTransferAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var legs = await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.TransferId == transferId)
            .ToListAsync(cancellationToken);

        if (legs.Count == 0)
        {
            return;
        }

        if (legs.Count == 1)
        {
            _logger.LogWarning(
                "Orphaned transfer leg detected for TransferId {TransferId}. Deleting single leg (Id={TransactionId}).",
                transferId,
                legs[0].Id);

            _context.Transactions.Remove(legs[0]);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var leg in legs)
            {
                _context.Transactions.Remove(leg);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Applies ownership filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-user data leaks.
    /// </summary>
    private IQueryable<Transaction> ApplyScopeFilter(IQueryable<Transaction> query)
    {
        var userId = _userContext.UserIdAsGuid;

        if (userId is null)
        {
            return query.Where(t => t.OwnerUserId == null);
        }

        return query.Where(t => t.OwnerUserId == null || t.OwnerUserId == userId);
    }
}

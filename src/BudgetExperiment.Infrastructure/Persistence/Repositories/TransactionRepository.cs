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

        // Amount is stored as encrypted text at rest; perform numeric aggregation client-side.
        var transactions = await query
            .Select(t => new
            {
                t.Date,
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
            })
            .ToListAsync(cancellationToken);

        // Group by date and calculate totals.
        var dailyTotals = transactions
            .GroupBy(t => t.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                Currency = g.First().Currency,
                Count = g.Count(),
            })
            .OrderBy(d => d.Date)
            .ToList();

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

        var amounts = await this.ApplyScopeFilter(_context.Transactions)
            .Where(t => t.Date >= startDate
                && t.Date <= endDate
                && t.CategoryId == categoryId)
            .Select(t => t.Amount.Amount)
            .ToListAsync(cancellationToken);

        // Amount is stored as encrypted text at rest; perform numeric aggregation client-side.
        var totalSpending = amounts
            .Where(amount => amount < 0)
            .Sum(amount => Math.Abs(amount));

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

        var transactions = await this.ApplyScopeFilter(_context.Transactions)
            .AsNoTracking()
            .Where(t => t.Date >= startDate
                && t.Date <= endDate
                && t.TransferId == null
                && t.CategoryId != null)
            .Select(t => new
            {
                CategoryId = t.CategoryId,
                Amount = t.Amount.Amount,
            })
            .ToListAsync(cancellationToken);

        // Amount is stored as encrypted text at rest; perform numeric aggregation client-side.
        return transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(t => Math.Abs(t.Amount)));
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
        if (_context.HasEncryptionService)
        {
            // AES-GCM produces unique ciphertexts per encrypt, so SQL DISTINCT/ORDER BY on
            // encrypted columns never deduplicates or sorts correctly. Materialize the raw
            // encrypted rows and apply deduplication and ordering over decrypted values.
            var raw = await this.ApplyScopeFilter(_context.Transactions)
                .AsNoTracking()
                .Where(t => t.CategoryId == null)
                .Select(t => t.Description)
                .ToListAsync(cancellationToken);

            return raw
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();
        }

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

        var hasEncryptedConverters = _context.HasEncryptionService;

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        var normalizedDescriptionFilter = descriptionContains?.Trim();
        if (!hasEncryptedConverters && !string.IsNullOrWhiteSpace(normalizedDescriptionFilter))
        {
            query = query.Where(t => t.Description.ToLower().Contains(normalizedDescriptionFilter.ToLower()));
        }

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        var uncategorized = await query.ToListAsync(cancellationToken);

        // Amount is stored as encrypted text at rest; perform sensitive filters/sort client-side.
        IEnumerable<Transaction> filtered = uncategorized;

        if (hasEncryptedConverters && !string.IsNullOrWhiteSpace(normalizedDescriptionFilter))
        {
            filtered = filtered.Where(t => t.Description.Contains(normalizedDescriptionFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (minAmount.HasValue)
        {
            var min = minAmount.Value;
            filtered = filtered.Where(t => Math.Abs(t.Amount.Amount) >= min);
        }

        if (maxAmount.HasValue)
        {
            var max = maxAmount.Value;
            filtered = filtered.Where(t => Math.Abs(t.Amount.Amount) <= max);
        }

        filtered = sortBy.ToUpperInvariant() switch
        {
            "AMOUNT" => sortDescending
                ? filtered.OrderByDescending(t => Math.Abs(t.Amount.Amount)).ThenByDescending(t => t.Date)
                : filtered.OrderBy(t => Math.Abs(t.Amount.Amount)).ThenBy(t => t.Date),
            "DESCRIPTION" => sortDescending
                ? filtered.OrderByDescending(t => t.Description).ThenByDescending(t => t.Date)
                : filtered.OrderBy(t => t.Description).ThenBy(t => t.Date),
            _ => sortDescending
                ? filtered.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAtUtc)
                : filtered.OrderBy(t => t.Date).ThenBy(t => t.CreatedAtUtc),
        };

        var materialized = filtered.ToList();
        var totalCount = materialized.Count;
        var items = materialized
            .Skip(skip)
            .Take(take)
            .ToList();

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
        if (_context.HasEncryptionService)
        {
            // SQL StartsWith on encrypted ciphertext will never match plaintext prefixes.
            // Materialize all descriptions (decrypted by the EF converter) and apply
            // deduplication, prefix filter, and ordering on the decrypted values.
            var raw = await this.ApplyScopeFilter(_context.Transactions)
                .AsNoTracking()
                .Select(t => t.Description)
                .ToListAsync(cancellationToken);

            IEnumerable<string> decrypted = raw
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(searchPrefix))
            {
                decrypted = decrypted.Where(d => d.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase));
            }

            return decrypted
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .ToList();
        }

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

        var amounts = await query
            .Select(t => new
            {
                Currency = t.Amount.Currency,
                Amount = t.Amount.Amount,
            })
            .ToListAsync(ct);

        // Amount is stored as encrypted text at rest; perform numeric aggregation client-side.
        var result = amounts
            .GroupBy(t => t.Currency)
            .Select(group => new
            {
                Currency = group.Key,
                Total = group.Sum(x => x.Amount),
            })
            .FirstOrDefault();

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
        var hasEncryptedConverters = _context.HasEncryptionService;
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

        var normalizedDescriptionFilter = descriptionContains?.Trim();
        if (!hasEncryptedConverters && !string.IsNullOrWhiteSpace(normalizedDescriptionFilter))
        {
            query = query.Where(t => t.Description.ToLower().Contains(normalizedDescriptionFilter.ToLower()));
        }

        if (!hasEncryptedConverters && minAmount.HasValue)
        {
            var min = minAmount.Value;
            query = query.Where(t => (t.Amount.Amount >= 0 ? t.Amount.Amount : -t.Amount.Amount) >= min);
        }

        if (!hasEncryptedConverters && maxAmount.HasValue)
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

        if (!hasEncryptedConverters)
        {
            // Get total count before paging.
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting.
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

            // Secondary sort for stability.
            var items = await orderedQuery
                .ThenByDescending(t => t.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        // In encrypted mode, apply sensitive filters and account-name sorting in-memory.
        IEnumerable<Transaction> filtered = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedDescriptionFilter))
        {
            filtered = filtered.Where(t => t.Description.Contains(normalizedDescriptionFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (minAmount.HasValue)
        {
            var min = minAmount.Value;
            filtered = filtered.Where(t => Math.Abs(t.Amount.Amount) >= min);
        }

        if (maxAmount.HasValue)
        {
            var max = maxAmount.Value;
            filtered = filtered.Where(t => Math.Abs(t.Amount.Amount) <= max);
        }

        var accountNames = await this.ApplyAccountScopeFilter(_context.Accounts)
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var ordered = sortBy.ToUpperInvariant() switch
        {
            "AMOUNT" => sortDescending
                ? filtered.OrderByDescending(t => Math.Abs(t.Amount.Amount))
                : filtered.OrderBy(t => Math.Abs(t.Amount.Amount)),
            "DESCRIPTION" => sortDescending
                ? filtered.OrderByDescending(t => t.Description)
                : filtered.OrderBy(t => t.Description),
            "CATEGORY" => sortDescending
                ? filtered.OrderByDescending(t => t.Category?.Name ?? string.Empty)
                : filtered.OrderBy(t => t.Category?.Name ?? string.Empty),
            "ACCOUNT" => sortDescending
                ? filtered.OrderByDescending(t => accountNames.GetValueOrDefault(t.AccountId, string.Empty))
                : filtered.OrderBy(t => accountNames.GetValueOrDefault(t.AccountId, string.Empty)),
            _ => sortDescending
                ? filtered.OrderByDescending(t => t.Date)
                : filtered.OrderBy(t => t.Date),
        };

        var materialized = ordered
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToList();

        var inMemoryTotalCount = materialized.Count;
        var pagedItems = materialized
            .Skip(skip)
            .Take(take)
            .ToList();

        return (pagedItems, inMemoryTotalCount);
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

    private IQueryable<Account> ApplyAccountScopeFilter(IQueryable<Account> query)
    {
        var userId = _userContext.UserIdAsGuid;

        if (userId is null)
        {
            return query.Where(a => a.OwnerUserId == null);
        }

        return query.Where(a => a.OwnerUserId == null || a.OwnerUserId == userId);
    }
}

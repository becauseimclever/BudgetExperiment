// <copyright file="CategorizationRuleRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICategorizationRuleRepository"/>.
/// </summary>
internal sealed class CategorizationRuleRepository : ICategorizationRuleRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRuleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CategorizationRuleRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CategorizationRule entity, CancellationToken cancellationToken = default)
    {
        await _context.CategorizationRules.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(CategorizationRule entity, CancellationToken cancellationToken = default)
    {
        _context.CategorizationRules.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> GetActiveByPriorityAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.CategoryId == categoryId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetNextPriorityAsync(CancellationToken cancellationToken = default)
    {
        var maxPriority = await _context.CategorizationRules
            .MaxAsync(r => (int?)r.Priority, cancellationToken);

        return (maxPriority ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task ReorderPrioritiesAsync(IEnumerable<(Guid RuleId, int NewPriority)> priorities, CancellationToken cancellationToken = default)
    {
        var priorityList = priorities.ToList();
        var ruleIds = priorityList.Select(p => p.RuleId).ToList();

        var rules = await _context.CategorizationRules
            .Where(r => ruleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var newPriority = priorityList.First(p => p.RuleId == rule.Id).NewPriority;
            rule.SetPriority(newPriority);
        }
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CategorizationRule> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CategorizationRules
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .AsQueryable();

        var normalizedSearch = search?.Trim();

        if (!_context.HasEncryptionService && !string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var searchLower = normalizedSearch.ToUpperInvariant();
            query = query.Where(r =>
                r.Name.ToUpper().Contains(searchLower) ||
                r.Pattern.ToUpper().Contains(searchLower));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        if (!_context.HasEncryptionService)
        {
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySort(query, sortBy, sortDirection);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        IEnumerable<CategorizationRule> filtered = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filtered = filtered.Where(r =>
                r.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                r.Pattern.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = ApplySort(filtered, sortBy, sortDirection).ToList();
        var inMemoryCount = sorted.Count;
        var pageItems = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pageItems, inMemoryCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveBulkAsync(IReadOnlyList<CategorizationRule> rules)
    {
        _context.CategorizationRules.RemoveRange(rules);
        return Task.CompletedTask;
    }

    private static IQueryable<CategorizationRule> ApplySort(
        IQueryable<CategorizationRule> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            "CATEGORY" => descending
                ? query.OrderByDescending(r => r.Category!.Name).ThenBy(r => r.Priority)
                : query.OrderBy(r => r.Category!.Name).ThenBy(r => r.Priority),
            "CREATEDAT" => descending
                ? query.OrderByDescending(r => r.CreatedAtUtc)
                : query.OrderBy(r => r.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Name)
                : query.OrderBy(r => r.Priority).ThenBy(r => r.Name),
        };
    }

    private static IOrderedEnumerable<CategorizationRule> ApplySort(
        IEnumerable<CategorizationRule> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            "CATEGORY" => descending
                ? query.OrderByDescending(r => r.Category?.Name ?? string.Empty).ThenBy(r => r.Priority)
                : query.OrderBy(r => r.Category?.Name ?? string.Empty).ThenBy(r => r.Priority),
            "CREATEDAT" => descending
                ? query.OrderByDescending(r => r.CreatedAtUtc)
                : query.OrderBy(r => r.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Name)
                : query.OrderBy(r => r.Priority).ThenBy(r => r.Name),
        };
    }
}

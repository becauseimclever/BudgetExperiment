// <copyright file="BudgetCategoryRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBudgetCategoryRepository"/>.
/// </summary>
internal sealed class BudgetCategoryRepository : IBudgetCategoryRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    public BudgetCategoryRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .Where(c => c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetCategories)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
    {
        await _context.BudgetCategories.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
    {
        _context.BudgetCategories.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies ownership filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-user data leaks.
    /// </summary>
    private IQueryable<BudgetCategory> ApplyScopeFilter(IQueryable<BudgetCategory> query)
    {
        var userId = _userContext.UserIdAsGuid;
        if (userId is null)
        {
            return query.Where(x => x.OwnerUserId == null);
        }

        return query.Where(x => x.OwnerUserId == null || x.OwnerUserId == userId);
    }
}

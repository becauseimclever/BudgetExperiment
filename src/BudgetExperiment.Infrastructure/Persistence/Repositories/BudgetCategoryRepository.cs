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
    /// <param name="userContext">The user context for scope filtering.</param>
    public BudgetCategoryRepository(BudgetDbContext context, IUserContext userContext)
    {
        this._context = context;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .Where(c => c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.BudgetCategories).LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
    {
        await this._context.BudgetCategories.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
    {
        this._context.BudgetCategories.Remove(entity);
        return Task.CompletedTask;
    }

    private IQueryable<BudgetCategory> ApplyScopeFilter(IQueryable<BudgetCategory> query)
    {
        var userId = this._userContext.UserIdAsGuid;
        return this._userContext.CurrentScope switch
        {
            BudgetScope.Shared => query.Where(x => x.Scope == BudgetScope.Shared),
            BudgetScope.Personal => query.Where(x => x.Scope == BudgetScope.Personal && x.OwnerUserId == userId),
            _ => query.Where(x => x.Scope == BudgetScope.Shared || (x.Scope == BudgetScope.Personal && x.OwnerUserId == userId)),
        };
    }
}

// <copyright file="BudgetCategoryRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBudgetCategoryRepository"/>.
/// </summary>
internal sealed class BudgetCategoryRepository : IBudgetCategoryRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BudgetCategoryRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .Where(c => c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetCategories.LongCountAsync(cancellationToken);
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
}

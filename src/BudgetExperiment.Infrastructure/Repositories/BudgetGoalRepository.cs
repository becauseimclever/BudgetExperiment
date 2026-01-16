// <copyright file="BudgetGoalRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBudgetGoalRepository"/>.
/// </summary>
internal sealed class BudgetGoalRepository : IBudgetGoalRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoalRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BudgetGoalRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<BudgetGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BudgetGoal?> GetByCategoryAndMonthAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals
            .FirstOrDefaultAsync(g => g.CategoryId == categoryId && g.Year == year && g.Month == month, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals
            .Where(g => g.Year == year && g.Month == month)
            .Include(g => g.Category)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals
            .Where(g => g.CategoryId == categoryId)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.BudgetGoals.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BudgetGoal entity, CancellationToken cancellationToken = default)
    {
        await this._context.BudgetGoals.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(BudgetGoal entity, CancellationToken cancellationToken = default)
    {
        this._context.BudgetGoals.Remove(entity);
        return Task.CompletedTask;
    }
}

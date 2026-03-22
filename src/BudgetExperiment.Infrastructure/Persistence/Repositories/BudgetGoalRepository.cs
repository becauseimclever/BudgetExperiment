// <copyright file="BudgetGoalRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBudgetGoalRepository"/>.
/// </summary>
internal sealed class BudgetGoalRepository : IBudgetGoalRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoalRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for scope filtering.</param>
    public BudgetGoalRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<BudgetGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BudgetGoal?> GetByCategoryAndMonthAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .FirstOrDefaultAsync(g => g.CategoryId == categoryId && g.Year == year && g.Month == month, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .Where(g => g.Year == year && g.Month == month)
            .Include(g => g.Category)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .AsNoTracking()
            .Where(g => g.CategoryId == categoryId)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoal>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .AsNoTracking()
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.BudgetGoals)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BudgetGoal entity, CancellationToken cancellationToken = default)
    {
        await _context.BudgetGoals.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(BudgetGoal entity, CancellationToken cancellationToken = default)
    {
        _context.BudgetGoals.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies budget scope filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-scope data leaks.
    /// See Feature 065 for the audit that established this rule.
    /// </summary>
    private IQueryable<BudgetGoal> ApplyScopeFilter(IQueryable<BudgetGoal> query)
    {
        var userId = _userContext.UserIdAsGuid;
        return _userContext.CurrentScope switch
        {
            BudgetScope.Shared => query.Where(x => x.Scope == BudgetScope.Shared),
            BudgetScope.Personal => query.Where(x => x.Scope == BudgetScope.Personal && x.OwnerUserId == userId),
            _ => query.Where(x => x.Scope == BudgetScope.Shared || (x.Scope == BudgetScope.Personal && x.OwnerUserId == userId)),
        };
    }
}

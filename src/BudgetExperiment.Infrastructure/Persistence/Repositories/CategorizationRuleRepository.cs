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
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.CategorizationRules
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.CategorizationRules
            .Include(r => r.Category)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.CategorizationRules.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CategorizationRule entity, CancellationToken cancellationToken = default)
    {
        await this._context.CategorizationRules.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(CategorizationRule entity, CancellationToken cancellationToken = default)
    {
        this._context.CategorizationRules.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> GetActiveByPriorityAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.CategorizationRules
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRule>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await this._context.CategorizationRules
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetNextPriorityAsync(CancellationToken cancellationToken = default)
    {
        var maxPriority = await this._context.CategorizationRules
            .MaxAsync(r => (int?)r.Priority, cancellationToken);

        return (maxPriority ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task ReorderPrioritiesAsync(IEnumerable<(Guid RuleId, int NewPriority)> priorities, CancellationToken cancellationToken = default)
    {
        var priorityList = priorities.ToList();
        var ruleIds = priorityList.Select(p => p.RuleId).ToList();

        var rules = await this._context.CategorizationRules
            .Where(r => ruleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var newPriority = priorityList.First(p => p.RuleId == rule.Id).NewPriority;
            rule.SetPriority(newPriority);
        }
    }
}

// <copyright file="KaizenGoalRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Kaizen;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IKaizenGoalRepository"/>.
/// </summary>
internal sealed class KaizenGoalRepository : IKaizenGoalRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenGoalRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public KaizenGoalRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<KaizenGoal?> GetByUserWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default)
    {
        return await _context.KaizenGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.WeekStartDate == weekStart, ct);
    }

    /// <inheritdoc />
    public async Task<KaizenGoal?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.KaizenGoals
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KaizenGoal>> GetRangeAsync(
        Guid userId,
        DateOnly fromWeek,
        DateOnly toWeek,
        CancellationToken ct = default)
    {
        return await _context.KaizenGoals
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.WeekStartDate >= fromWeek && g.WeekStartDate <= toWeek)
            .OrderByDescending(g => g.WeekStartDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(KaizenGoal goal, CancellationToken ct = default)
    {
        await _context.KaizenGoals.AddAsync(goal, ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(KaizenGoal goal, CancellationToken ct = default)
    {
        _context.KaizenGoals.Remove(goal);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}

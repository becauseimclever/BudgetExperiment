// <copyright file="MonthlyReflectionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Reflection;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMonthlyReflectionRepository"/>.
/// </summary>
internal sealed class MonthlyReflectionRepository : IMonthlyReflectionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyReflectionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MonthlyReflectionRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<MonthlyReflection?> GetByUserMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyReflections
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.Year == year && r.Month == month,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MonthlyReflection>> GetHistoryAsync(
        Guid userId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyReflections
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyReflections
            .AsNoTracking()
            .CountAsync(r => r.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MonthlyReflection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyReflections
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(MonthlyReflection reflection, CancellationToken cancellationToken = default)
    {
        await _context.MonthlyReflections.AddAsync(reflection, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(MonthlyReflection reflection)
    {
        _context.MonthlyReflections.Remove(reflection);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

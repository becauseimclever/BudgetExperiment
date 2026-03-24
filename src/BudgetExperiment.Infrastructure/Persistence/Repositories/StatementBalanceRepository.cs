// <copyright file="StatementBalanceRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IStatementBalanceRepository"/>.
/// </summary>
internal sealed class StatementBalanceRepository : IStatementBalanceRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementBalanceRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public StatementBalanceRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StatementBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StatementBalances
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StatementBalance>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.StatementBalances
            .AsNoTracking()
            .OrderByDescending(s => s.StatementDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StatementBalances
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(StatementBalance entity, CancellationToken cancellationToken = default)
    {
        await _context.StatementBalances.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(StatementBalance entity, CancellationToken cancellationToken = default)
    {
        _context.StatementBalances.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<StatementBalance?> GetActiveByAccountAsync(Guid accountId, CancellationToken ct)
    {
        return await _context.StatementBalances
            .FirstOrDefaultAsync(s => s.AccountId == accountId && !s.IsCompleted, ct);
    }
}

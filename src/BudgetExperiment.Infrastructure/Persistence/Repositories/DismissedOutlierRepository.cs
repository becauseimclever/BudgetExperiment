// <copyright file="DismissedOutlierRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDismissedOutlierRepository"/>.
/// </summary>
internal sealed class DismissedOutlierRepository : IDismissedOutlierRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DismissedOutlierRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DismissedOutlierRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<DismissedOutlier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DismissedOutliers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DismissedOutlier>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.DismissedOutliers
            .AsNoTracking()
            .OrderByDescending(d => d.DismissedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DismissedOutliers
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(DismissedOutlier entity, CancellationToken cancellationToken = default)
    {
        await _context.DismissedOutliers.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(DismissedOutlier entity, CancellationToken cancellationToken = default)
    {
        _context.DismissedOutliers.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> IsDismissedAsync(Guid transactionId, CancellationToken ct)
    {
        return await _context.DismissedOutliers
            .AsNoTracking()
            .AnyAsync(d => d.TransactionId == transactionId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetDismissedTransactionIdsAsync(CancellationToken ct)
    {
        return await _context.DismissedOutliers
            .AsNoTracking()
            .Select(d => d.TransactionId)
            .ToListAsync(ct);
    }
}

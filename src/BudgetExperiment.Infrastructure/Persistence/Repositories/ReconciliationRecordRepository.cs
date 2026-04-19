// <copyright file="ReconciliationRecordRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReconciliationRecordRepository"/>.
/// </summary>
internal sealed class ReconciliationRecordRepository : IReconciliationRecordRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationRecordRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    public ReconciliationRecordRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<ReconciliationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ApplyScopeFilter(_context.ReconciliationRecords)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationRecord>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await ApplyScopeFilter(_context.ReconciliationRecords)
            .AsNoTracking()
            .OrderByDescending(r => r.StatementDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await ApplyScopeFilter(_context.ReconciliationRecords)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ReconciliationRecord entity, CancellationToken cancellationToken = default)
    {
        await _context.ReconciliationRecords.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ReconciliationRecord entity, CancellationToken cancellationToken = default)
    {
        _context.ReconciliationRecords.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationRecord>> GetByAccountAsync(Guid accountId, CancellationToken ct)
    {
        return await ApplyScopeFilter(_context.ReconciliationRecords)
            .AsNoTracking()
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.StatementDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ReconciliationRecord?> GetLatestByAccountAsync(Guid accountId, CancellationToken ct)
    {
        return await ApplyScopeFilter(_context.ReconciliationRecords)
            .AsNoTracking()
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.StatementDate)
            .FirstOrDefaultAsync(ct);
    }

    private IQueryable<ReconciliationRecord> ApplyScopeFilter(IQueryable<ReconciliationRecord> query)
    {
        var userId = _userContext.UserIdAsGuid;

        if (userId is null)
        {
            return query.Where(r => r.OwnerUserId == null);
        }

        return query.Where(r => r.OwnerUserId == null || r.OwnerUserId == userId);
    }
}

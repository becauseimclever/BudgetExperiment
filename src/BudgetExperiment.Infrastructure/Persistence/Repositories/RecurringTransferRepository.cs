// <copyright file="RecurringTransferRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRecurringTransferRepository"/>.
/// </summary>
internal sealed class RecurringTransferRepository : IRecurringTransferRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    public RecurringTransferRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<RecurringTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecurringTransfer?> GetByIdWithExceptionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // RecurringTransfer doesn't have a navigation property to exceptions,
        // so we fetch them separately if needed. For now, just return the entity.
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .Where(r => r.SourceAccountId == accountId || r.DestinationAccountId == accountId)
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> GetBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .Where(r => r.SourceAccountId == sourceAccountId)
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> GetByDestinationAccountIdAsync(Guid destinationAccountId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .Where(r => r.DestinationAccountId == destinationAccountId)
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransfer>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .OrderBy(r => r.Description)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransfers)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RecurringTransfer entity, CancellationToken cancellationToken = default)
    {
        await _context.RecurringTransfers.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(RecurringTransfer entity, CancellationToken cancellationToken = default)
    {
        _context.RecurringTransfers.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransferException>> GetExceptionsByDateRangeAsync(
        Guid recurringTransferId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.RecurringTransferExceptions
            .AsNoTracking()
            .Where(e => e.RecurringTransferId == recurringTransferId)
            .Where(e => e.OriginalDate >= fromDate && e.OriginalDate <= toDate)
            .OrderBy(e => e.OriginalDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecurringTransferException?> GetExceptionAsync(
        Guid recurringTransferId,
        DateOnly originalDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.RecurringTransferExceptions
            .FirstOrDefaultAsync(
                e => e.RecurringTransferId == recurringTransferId && e.OriginalDate == originalDate,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddExceptionAsync(RecurringTransferException exception, CancellationToken cancellationToken = default)
    {
        await _context.RecurringTransferExceptions.AddAsync(exception, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveExceptionAsync(RecurringTransferException exception, CancellationToken cancellationToken = default)
    {
        _context.RecurringTransferExceptions.Remove(exception);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveExceptionsFromDateAsync(
        Guid recurringTransferId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var exceptionsToRemove = await _context.RecurringTransferExceptions
            .Where(e => e.RecurringTransferId == recurringTransferId)
            .Where(e => e.OriginalDate >= fromDate)
            .ToListAsync(cancellationToken);

        _context.RecurringTransferExceptions.RemoveRange(exceptionsToRemove);
    }

    /// <summary>
    /// Applies ownership filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-user data leaks.
    /// </summary>
    private IQueryable<RecurringTransfer> ApplyScopeFilter(IQueryable<RecurringTransfer> query)
    {
        var userId = _userContext.UserIdAsGuid;
        if (userId is null)
        {
            return query.Where(x => x.OwnerUserId == null);
        }

        return query.Where(x => x.OwnerUserId == null || x.OwnerUserId == userId);
    }
}

// <copyright file="RecurringTransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRecurringTransactionRepository"/>.
/// </summary>
internal sealed class RecurringTransactionRepository : IRecurringTransactionRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    public RecurringTransactionRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<RecurringTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecurringTransaction?> GetByIdWithExceptionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // RecurringTransaction doesn't have a navigation property to exceptions,
        // so we fetch them separately if needed. For now, just return the entity.
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .Where(r => r.IsActive)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(r => r.Description)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RecurringTransaction entity, CancellationToken cancellationToken = default)
    {
        await _context.RecurringTransactions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(RecurringTransaction entity, CancellationToken cancellationToken = default)
    {
        _context.RecurringTransactions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransactionException>> GetExceptionsByDateRangeAsync(
        Guid recurringTransactionId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.RecurringTransactionExceptions
            .AsNoTracking()
            .Where(e => e.RecurringTransactionId == recurringTransactionId)
            .Where(e => e.OriginalDate >= fromDate && e.OriginalDate <= toDate)
            .OrderBy(e => e.OriginalDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionException?> GetExceptionAsync(
        Guid recurringTransactionId,
        DateOnly originalDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.RecurringTransactionExceptions
            .FirstOrDefaultAsync(
                e => e.RecurringTransactionId == recurringTransactionId && e.OriginalDate == originalDate,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddExceptionAsync(RecurringTransactionException exception, CancellationToken cancellationToken = default)
    {
        await _context.RecurringTransactionExceptions.AddAsync(exception, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveExceptionAsync(RecurringTransactionException exception, CancellationToken cancellationToken = default)
    {
        _context.RecurringTransactionExceptions.Remove(exception);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveExceptionsFromDateAsync(
        Guid recurringTransactionId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var exceptionsToRemove = await _context.RecurringTransactionExceptions
            .Where(e => e.RecurringTransactionId == recurringTransactionId)
            .Where(e => e.OriginalDate >= fromDate)
            .ToListAsync(cancellationToken);

        _context.RecurringTransactionExceptions.RemoveRange(exceptionsToRemove);
    }

    /// <summary>
    /// Applies ownership filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-user data leaks.
    /// </summary>
    private IQueryable<RecurringTransaction> ApplyScopeFilter(IQueryable<RecurringTransaction> query)
    {
        var userId = _userContext.UserIdAsGuid;
        if (userId is null)
        {
            return query.Where(x => x.OwnerUserId == null);
        }

        return query.Where(x => x.OwnerUserId == null || x.OwnerUserId == userId);
    }
}

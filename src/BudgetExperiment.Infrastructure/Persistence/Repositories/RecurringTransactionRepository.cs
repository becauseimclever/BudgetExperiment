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
    /// <param name="userContext">The user context for scope filtering.</param>
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
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Description)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions)
            .Include(r => r.Category)
            .OrderBy(r => r.Description)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.RecurringTransactions).LongCountAsync(cancellationToken);
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
    /// Applies budget scope filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-scope data leaks.
    /// See Feature 065 for the audit that established this rule.
    /// </summary>
    private IQueryable<RecurringTransaction> ApplyScopeFilter(IQueryable<RecurringTransaction> query)
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

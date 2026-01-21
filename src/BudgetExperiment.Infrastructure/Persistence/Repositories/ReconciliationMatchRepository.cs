// <copyright file="ReconciliationMatchRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReconciliationMatchRepository"/>.
/// </summary>
internal sealed class ReconciliationMatchRepository : IReconciliationMatchRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationMatchRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for scope filtering.</param>
    public ReconciliationMatchRepository(BudgetDbContext context, IUserContext userContext)
    {
        this._context = context;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<ReconciliationMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatch>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ReconciliationMatch entity, CancellationToken cancellationToken = default)
    {
        await this._context.ReconciliationMatches.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ReconciliationMatch entity, CancellationToken cancellationToken = default)
    {
        this._context.ReconciliationMatches.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatch>> GetPendingMatchesAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .Where(m => m.Status == ReconciliationMatchStatus.Suggested)
            .OrderByDescending(m => m.ConfidenceScore)
            .ThenByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatch>> GetByRecurringTransactionAsync(
        Guid recurringTransactionId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .Where(m => m.RecurringTransactionId == recurringTransactionId)
            .Where(m => m.RecurringInstanceDate >= startDate && m.RecurringInstanceDate <= endDate)
            .OrderBy(m => m.RecurringInstanceDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatch>> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .Where(m => m.ImportedTransactionId == transactionId)
            .OrderByDescending(m => m.ConfidenceScore)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatch>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await this.ApplyScopeFilter(this._context.ReconciliationMatches)
            .Where(m => m.RecurringInstanceDate >= startDate && m.RecurringInstanceDate <= endDate)
            .OrderBy(m => m.RecurringInstanceDate)
            .ThenByDescending(m => m.ConfidenceScore)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Guid transactionId,
        Guid recurringTransactionId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default)
    {
        return await this._context.ReconciliationMatches
            .AnyAsync(
                m => m.ImportedTransactionId == transactionId
                    && m.RecurringTransactionId == recurringTransactionId
                    && m.RecurringInstanceDate == instanceDate,
                cancellationToken);
    }

    private IQueryable<ReconciliationMatch> ApplyScopeFilter(IQueryable<ReconciliationMatch> query)
    {
        var userId = this._userContext.UserIdAsGuid;

        // Show shared matches and user's personal matches
        return query.Where(m =>
            m.Scope == BudgetScope.Shared ||
            (m.Scope == BudgetScope.Personal && m.OwnerUserId == userId));
    }
}

// <copyright file="RecurringChargeSuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRecurringChargeSuggestionRepository"/>.
/// </summary>
internal sealed class RecurringChargeSuggestionRepository : IRecurringChargeSuggestionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public RecurringChargeSuggestionRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<RecurringChargeSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.RecurringChargeSuggestions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringChargeSuggestion>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.RecurringChargeSuggestions
            .OrderByDescending(s => s.Confidence)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.RecurringChargeSuggestions.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RecurringChargeSuggestion entity, CancellationToken cancellationToken = default)
    {
        await this._context.RecurringChargeSuggestions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(RecurringChargeSuggestion entity, CancellationToken cancellationToken = default)
    {
        this._context.RecurringChargeSuggestions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringChargeSuggestion>> GetByStatusAsync(
        Guid? accountId,
        SuggestionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = this._context.RecurringChargeSuggestions.AsQueryable();

        if (accountId.HasValue)
        {
            query = query.Where(s => s.AccountId == accountId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query
            .OrderByDescending(s => s.Confidence)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountByStatusAsync(
        Guid? accountId,
        SuggestionStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = this._context.RecurringChargeSuggestions.AsQueryable();

        if (accountId.HasValue)
        {
            query = query.Where(s => s.AccountId == accountId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecurringChargeSuggestion?> GetByNormalizedDescriptionAndAccountAsync(
        string normalizedDescription,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await this._context.RecurringChargeSuggestions
            .FirstOrDefaultAsync(
                s => s.NormalizedDescription == normalizedDescription
                    && s.AccountId == accountId
                    && s.Status != SuggestionStatus.Accepted,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<RecurringChargeSuggestion> suggestions,
        CancellationToken cancellationToken = default)
    {
        await this._context.RecurringChargeSuggestions.AddRangeAsync(suggestions, cancellationToken);
    }
}

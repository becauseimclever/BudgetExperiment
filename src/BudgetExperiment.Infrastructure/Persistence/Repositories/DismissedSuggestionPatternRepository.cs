// <copyright file="DismissedSuggestionPatternRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDismissedSuggestionPatternRepository"/>.
/// </summary>
internal sealed class DismissedSuggestionPatternRepository : IDismissedSuggestionPatternRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DismissedSuggestionPatternRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DismissedSuggestionPatternRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<DismissedSuggestionPattern?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DismissedSuggestionPatterns
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DismissedSuggestionPattern>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.DismissedSuggestionPatterns
            .OrderByDescending(p => p.DismissedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DismissedSuggestionPatterns.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(DismissedSuggestionPattern entity, CancellationToken cancellationToken = default)
    {
        await _context.DismissedSuggestionPatterns.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(DismissedSuggestionPattern entity, CancellationToken cancellationToken = default)
    {
        _context.DismissedSuggestionPatterns.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> IsDismissedAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
    {
        var normalizedPattern = pattern.Trim().ToUpperInvariant();
        return await _context.DismissedSuggestionPatterns
            .AnyAsync(p => p.OwnerId == ownerId && p.Pattern == normalizedPattern, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DismissedSuggestionPattern>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.DismissedSuggestionPatterns
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.DismissedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DismissedSuggestionPattern?> GetByPatternAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
    {
        var normalizedPattern = pattern.Trim().ToUpperInvariant();
        return await _context.DismissedSuggestionPatterns
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.Pattern == normalizedPattern, cancellationToken);
    }
}

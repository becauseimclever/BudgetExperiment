// <copyright file="CategorySuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICategorySuggestionRepository"/>.
/// </summary>
internal sealed class CategorySuggestionRepository : ICategorySuggestionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CategorySuggestionRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CategorySuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CategorySuggestions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.CategorySuggestions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CategorySuggestions.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CategorySuggestion entity, CancellationToken cancellationToken = default)
    {
        await _context.CategorySuggestions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(CategorySuggestion entity, CancellationToken cancellationToken = default)
    {
        _context.CategorySuggestions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> GetPendingByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.CategorySuggestions
            .Where(s => s.OwnerId == ownerId && s.Status == SuggestionStatus.Pending)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> GetByStatusAsync(
        string ownerId,
        SuggestionStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.CategorySuggestions
            .Where(s => s.OwnerId == ownerId && s.Status == status)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingWithNameAsync(string ownerId, string suggestedName, CancellationToken cancellationToken = default)
    {
        var normalizedName = suggestedName.Trim().ToUpperInvariant();
        return await _context.CategorySuggestions
            .AnyAsync(
                s => s.OwnerId == ownerId
                    && s.Status == SuggestionStatus.Pending
                    && s.SuggestedName.ToUpper() == normalizedName,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<CategorySuggestion> suggestions, CancellationToken cancellationToken = default)
    {
        await _context.CategorySuggestions.AddRangeAsync(suggestions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeletePendingByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync is not supported by the InMemory provider, so we use RemoveRange instead
        var pendingSuggestions = await _context.CategorySuggestions
            .Where(s => s.OwnerId == ownerId && s.Status == SuggestionStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingSuggestions.Count > 0)
        {
            _context.CategorySuggestions.RemoveRange(pendingSuggestions);
        }
    }
}

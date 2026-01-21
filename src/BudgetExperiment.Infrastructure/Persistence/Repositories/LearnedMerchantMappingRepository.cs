// <copyright file="LearnedMerchantMappingRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILearnedMerchantMappingRepository"/>.
/// </summary>
internal sealed class LearnedMerchantMappingRepository : ILearnedMerchantMappingRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LearnedMerchantMappingRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LearnedMerchantMappingRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<LearnedMerchantMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LearnedMerchantMappings
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LearnedMerchantMapping>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.LearnedMerchantMappings
            .OrderByDescending(m => m.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LearnedMerchantMappings.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(LearnedMerchantMapping entity, CancellationToken cancellationToken = default)
    {
        await _context.LearnedMerchantMappings.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(LearnedMerchantMapping entity, CancellationToken cancellationToken = default)
    {
        _context.LearnedMerchantMappings.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<LearnedMerchantMapping?> GetByPatternAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
    {
        var normalizedPattern = pattern.Trim().ToUpperInvariant();
        return await _context.LearnedMerchantMappings
            .FirstOrDefaultAsync(m => m.OwnerId == ownerId && m.MerchantPattern == normalizedPattern, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LearnedMerchantMapping>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.LearnedMerchantMappings
            .Where(m => m.OwnerId == ownerId)
            .OrderByDescending(m => m.LearnCount)
            .ThenBy(m => m.MerchantPattern)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LearnedMerchantMapping>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.LearnedMerchantMappings
            .Where(m => m.CategoryId == categoryId)
            .OrderBy(m => m.MerchantPattern)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string ownerId, string pattern, CancellationToken cancellationToken = default)
    {
        var normalizedPattern = pattern.Trim().ToUpperInvariant();
        return await _context.LearnedMerchantMappings
            .AnyAsync(m => m.OwnerId == ownerId && m.MerchantPattern == normalizedPattern, cancellationToken);
    }
}

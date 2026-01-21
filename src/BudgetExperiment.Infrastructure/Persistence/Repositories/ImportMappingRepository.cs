// <copyright file="ImportMappingRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IImportMappingRepository"/>.
/// </summary>
internal sealed class ImportMappingRepository : IImportMappingRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportMappingRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ImportMappingRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<ImportMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportMappings
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportMapping>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportMappings
            .OrderByDescending(m => m.LastUsedAtUtc)
            .ThenBy(m => m.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.ImportMappings.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ImportMapping entity, CancellationToken cancellationToken = default)
    {
        await this._context.ImportMappings.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ImportMapping entity, CancellationToken cancellationToken = default)
    {
        this._context.ImportMappings.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportMapping>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportMappings
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.LastUsedAtUtc)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImportMapping?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportMappings
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Name == name, cancellationToken);
    }
}

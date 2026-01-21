// <copyright file="ImportBatchRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IImportBatchRepository"/>.
/// </summary>
internal sealed class ImportBatchRepository : IImportBatchRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportBatchRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ImportBatchRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatch>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches
            .OrderByDescending(b => b.ImportedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ImportBatch entity, CancellationToken cancellationToken = default)
    {
        await this._context.ImportBatches.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ImportBatch entity, CancellationToken cancellationToken = default)
    {
        this._context.ImportBatches.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatch>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.ImportedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatch>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches
            .Where(b => b.AccountId == accountId)
            .OrderByDescending(b => b.ImportedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatch>> GetByMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        return await this._context.ImportBatches
            .Where(b => b.MappingId == mappingId)
            .OrderByDescending(b => b.ImportedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

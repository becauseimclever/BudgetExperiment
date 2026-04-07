// <copyright file="FeatureFlagRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.FeatureFlags;
using BudgetExperiment.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFeatureFlagRepository"/>.
/// </summary>
internal sealed class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public FeatureFlagRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.FeatureFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(string name, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var flag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);

        if (flag is null)
        {
            return;
        }

        flag.IsEnabled = isEnabled;
        flag.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// <copyright file="AppSettingsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAppSettingsRepository"/>.
/// </summary>
internal sealed class AppSettingsRepository : IAppSettingsRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AppSettingsRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, cancellationToken);

        if (settings is null)
        {
            // Create default settings if not seeded
            settings = AppSettings.CreateDefault();
            _context.AppSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    /// <inheritdoc />
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        // Entity is tracked, SaveChangesAsync via UnitOfWork handles persistence
        return Task.CompletedTask;
    }
}

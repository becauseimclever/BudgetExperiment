// <copyright file="UserSettingsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserSettingsRepository"/>.
/// </summary>
internal sealed class UserSettingsRepository : IUserSettingsRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserSettingsRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<UserSettings> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await this._context.Set<UserSettings>()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            // Create default settings for this user
            settings = UserSettings.CreateDefault(userId);
            await this._context.Set<UserSettings>().AddAsync(settings, cancellationToken);
        }

        return settings;
    }

    /// <inheritdoc />
    public Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        // EF Core tracks changes automatically, just ensure the entity is tracked
        this._context.Set<UserSettings>().Update(settings);
        return Task.CompletedTask;
    }
}

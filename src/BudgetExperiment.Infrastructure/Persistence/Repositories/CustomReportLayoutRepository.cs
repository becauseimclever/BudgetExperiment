// <copyright file="CustomReportLayoutRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICustomReportLayoutRepository"/>.
/// </summary>
internal sealed class CustomReportLayoutRepository : ICustomReportLayoutRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportLayoutRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for scope filtering.</param>
    public CustomReportLayoutRepository(BudgetDbContext context, IUserContext userContext)
    {
        this._context = context;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<CustomReportLayout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.CustomReportLayouts)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomReportLayout>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.CustomReportLayouts)
            .OrderByDescending(l => l.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomReportLayout>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.CustomReportLayouts)
            .OrderByDescending(l => l.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.CustomReportLayouts)
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CustomReportLayout entity, CancellationToken cancellationToken = default)
    {
        await this._context.CustomReportLayouts.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(CustomReportLayout entity, CancellationToken cancellationToken = default)
    {
        this._context.CustomReportLayouts.Remove(entity);
        return Task.CompletedTask;
    }

    private IQueryable<CustomReportLayout> ApplyScopeFilter(IQueryable<CustomReportLayout> query)
    {
        var userId = this._userContext.UserIdAsGuid;
        return this._userContext.CurrentScope switch
        {
            BudgetScope.Shared => query.Where(l => l.Scope == BudgetScope.Shared),
            BudgetScope.Personal => query.Where(l => l.Scope == BudgetScope.Personal && l.OwnerUserId == userId),
            _ => query.Where(l => l.Scope == BudgetScope.Shared || (l.Scope == BudgetScope.Personal && l.OwnerUserId == userId)),
        };
    }
}

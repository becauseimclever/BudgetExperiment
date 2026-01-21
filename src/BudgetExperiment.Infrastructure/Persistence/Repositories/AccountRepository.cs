// <copyright file="AccountRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class AccountRepository : IAccountRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for scope filtering.</param>
    public AccountRepository(BudgetDbContext context, IUserContext userContext)
    {
        this._context = context;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.Accounts)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.Accounts)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.Accounts)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.Accounts)
            .OrderBy(a => a.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(this._context.Accounts).LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Account entity, CancellationToken cancellationToken = default)
    {
        await this._context.Accounts.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(Account entity, CancellationToken cancellationToken = default)
    {
        this._context.Accounts.Remove(entity);
        return Task.CompletedTask;
    }

    private IQueryable<Account> ApplyScopeFilter(IQueryable<Account> query)
    {
        var userId = this._userContext.UserIdAsGuid;

        return this._userContext.CurrentScope switch
        {
            BudgetScope.Shared => query.Where(a => a.Scope == BudgetScope.Shared),
            BudgetScope.Personal => query.Where(a => a.Scope == BudgetScope.Personal && a.OwnerUserId == userId),
            _ => query.Where(a =>
                a.Scope == BudgetScope.Shared ||
                (a.Scope == BudgetScope.Personal && a.OwnerUserId == userId)),
        };
    }
}

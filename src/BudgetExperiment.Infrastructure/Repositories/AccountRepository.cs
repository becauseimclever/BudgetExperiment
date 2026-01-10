// <copyright file="AccountRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class AccountRepository : IAccountRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AccountRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.Accounts
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.Accounts
            .OrderBy(a => a.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.Accounts.LongCountAsync(cancellationToken);
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
}

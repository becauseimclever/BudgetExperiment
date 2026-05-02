// <copyright file="AccountRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class AccountRepository : IAccountRepository, IAccountTransactionRangeRepository, IAccountNameLookupRepository
{
    private readonly BudgetDbContext _context;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContext">The user context for ownership filtering.</param>
    public AccountRepository(BudgetDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Accounts)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Accounts)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdWithTransactionsAsync(
        Guid id,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Accounts)
            .Include(a => a.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = this.ApplyScopeFilter(_context.Accounts)
            .AsNoTracking();

        if (!_context.HasEncryptionService)
        {
            return await query
                .OrderBy(a => a.Name)
                .ToListAsync(cancellationToken);
        }

        var accounts = await query.ToListAsync(cancellationToken);
        return accounts
            .OrderBy(a => a.Name)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var names = await this.ApplyScopeFilter(_context.Accounts)
            .AsNoTracking()
            .Where(a => idList.Contains(a.Id))
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(cancellationToken);

        return names.ToDictionary(n => n.Id, n => n.Name);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = this.ApplyScopeFilter(_context.Accounts)
            .AsNoTracking();

        if (!_context.HasEncryptionService)
        {
            return await query
                .OrderBy(a => a.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        var accounts = await query.ToListAsync(cancellationToken);
        return accounts
            .OrderBy(a => a.Name)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this.ApplyScopeFilter(_context.Accounts)
            .AsNoTracking()
            .LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Account entity, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(Account entity, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies ownership filtering to a query. IMPORTANT: Every public query method
    /// in this repository MUST call this method to prevent cross-user data leaks.
    /// </summary>
    private IQueryable<Account> ApplyScopeFilter(IQueryable<Account> query)
    {
        var userId = _userContext.UserIdAsGuid;

        if (userId is null)
        {
            return query.Where(a => a.OwnerUserId == null);
        }

        return query.Where(a => a.OwnerUserId == null || a.OwnerUserId == userId);
    }
}

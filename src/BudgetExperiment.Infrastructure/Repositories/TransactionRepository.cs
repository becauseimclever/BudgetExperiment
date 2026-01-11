// <copyright file="TransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITransactionRepository"/>.
/// </summary>
internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TransactionRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.Transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.Transactions.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = this._context.Transactions
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        return await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyTotal>> GetDailyTotalsAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = this._context.Transactions
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        // Group by date and calculate totals
        // Note: This assumes all transactions use the same currency (USD)
        // For multi-currency support, this would need to be grouped by currency too
        var dailyTotals = await query
            .GroupBy(t => t.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(t => t.Amount.Amount),
                Currency = g.First().Amount.Currency,
                Count = g.Count(),
            })
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

        return dailyTotals
            .Select(d => new DailyTotal(
                d.Date,
                MoneyValue.Create(d.Currency, d.TotalAmount),
                d.Count))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        return await this._context.Transactions
            .Where(t => t.TransferId == transferId)
            .OrderBy(t => t.TransferDirection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(Transaction entity, CancellationToken cancellationToken = default)
    {
        this._context.Transactions.Add(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(Transaction entity, CancellationToken cancellationToken = default)
    {
        this._context.Transactions.Remove(entity);
        return Task.CompletedTask;
    }
}

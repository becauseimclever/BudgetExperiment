using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of adhoc transaction read repository.
/// </summary>
public sealed class AdhocTransactionReadRepository : IAdhocTransactionReadRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdhocTransactionReadRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public AdhocTransactionReadRepository(BudgetDbContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<AdhocTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.AdhocTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransaction>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await this._context.AdhocTransactions
            .Where(t => t.Date == date)
            .OrderBy(t => t.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransaction>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await this._context.AdhocTransactions
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetIncomeTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = this._context.AdhocTransactions
            .Where(t => t.TransactionType == TransactionType.Income);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetExpenseTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = this._context.AdhocTransactions
            .Where(t => t.TransactionType == TransactionType.Expense);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.AdhocTransactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.AdhocTransactions
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransaction>> FindDuplicatesAsync(
        DateOnly date,
        string description,
        decimal amount,
        TransactionType transactionType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Array.Empty<AdhocTransaction>();
        }

        var trimmedDescription = description.Trim();

        // Query for potential duplicates: exact date, case-insensitive description, exact amount, same type
        return await this._context.AdhocTransactions
            .Where(t => t.Date == date
                && EF.Functions.Like(t.Description.ToLower(), trimmedDescription.ToLower())
                && Math.Abs(t.Money.Amount) == amount
                && t.TransactionType == transactionType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

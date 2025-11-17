namespace BudgetExperiment.Domain;

/// <summary>
/// Read repository interface for adhoc transactions.
/// </summary>
public interface IAdhocTransactionReadRepository
{
    /// <summary>
    /// Gets an adhoc transaction by its ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction if found, otherwise null.</returns>
    Task<AdhocTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all adhoc transactions for a specific date.
    /// </summary>
    /// <param name="date">The date to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of transactions on the specified date.</returns>
    Task<IReadOnlyList<AdhocTransaction>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all adhoc transactions within a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of transactions within the date range.</returns>
    Task<IReadOnlyList<AdhocTransaction>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets income transactions only (paged).
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Income transactions and total count.</returns>
    Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetIncomeTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense transactions only (paged).
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expense transactions and total count.</returns>
    Task<(IReadOnlyList<AdhocTransaction> Items, int Total)> GetExpenseTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all adhoc transactions with pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of transactions.</returns>
    Task<IReadOnlyList<AdhocTransaction>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of all adhoc transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total count.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds potential duplicate transactions based on date, description, amount, and type.
    /// </summary>
    /// <param name="date">The transaction date to match.</param>
    /// <param name="description">The description to match (case-insensitive).</param>
    /// <param name="amount">The absolute amount to match.</param>
    /// <param name="transactionType">The transaction type to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of matching transactions (potential duplicates).</returns>
    Task<IReadOnlyList<AdhocTransaction>> FindDuplicatesAsync(
        DateOnly date,
        string description,
        decimal amount,
        TransactionType transactionType,
        CancellationToken cancellationToken = default);
}

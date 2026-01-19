// <copyright file="ITransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for Transaction entity.
/// Transactions are owned by Account aggregate, but we need direct queries for calendar views
/// and write operations for transfers.
/// </summary>
public interface ITransactionRepository : IReadRepository<Transaction>, IWriteRepository<Transaction>
{
    /// <summary>
    /// Gets transactions within a date range, optionally filtered by account.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions in the date range.</returns>
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily transaction totals for a month (for calendar summary view).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily totals as (date, total amount).</returns>
    Task<IReadOnlyList<DailyTotal>> GetDailyTotalsAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the paired transactions for a transfer by the transfer identifier.
    /// </summary>
    /// <param name="transferId">The transfer identifier linking the paired transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The source and destination transactions for the transfer, or empty if not found.</returns>
    Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by recurring transaction ID and instance date.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The realized transaction or null if not found.</returns>
    Task<Transaction?> GetByRecurringInstanceAsync(
        Guid recurringTransactionId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions by recurring transfer ID and instance date.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The realized transfer transactions (source and destination) or empty if not found.</returns>
    Task<IReadOnlyList<Transaction>> GetByRecurringTransferInstanceAsync(
        Guid recurringTransferId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total spending for a category in a specific month.
    /// </summary>
    /// <param name="categoryId">The budget category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total spending amount for the category.</returns>
    Task<MoneyValue> GetSpendingByCategoryAsync(
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions that do not have a category assigned.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of uncategorized transactions.</returns>
    Task<IReadOnlyList<Transaction>> GetUncategorizedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique transaction descriptions for pattern analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of unique descriptions.</returns>
    Task<IReadOnlyList<string>> GetAllDescriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds potential duplicate transactions based on date, amount, and description.
    /// Used during import preview to detect existing transactions.
    /// </summary>
    /// <param name="accountId">The account to search in.</param>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions in the date range for duplicate matching.</returns>
    Task<IReadOnlyList<Transaction>> GetForDuplicateDetectionAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions from a specific import batch.
    /// </summary>
    /// <param name="batchId">The import batch ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions from the batch.</returns>
    Task<IReadOnlyList<Transaction>> GetByImportBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);
}

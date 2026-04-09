// <copyright file="ITransactionImportRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Focused repository interface for import-specific transaction operations including duplicate detection and batch queries.
/// </summary>
public interface ITransactionImportRepository
{
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

    /// <summary>
    /// Gets multiple transactions by their IDs in a single batch query.
    /// </summary>
    /// <param name="ids">The transaction IDs to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions matching the provided IDs.</returns>
    Task<IReadOnlyList<Transaction>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);
}

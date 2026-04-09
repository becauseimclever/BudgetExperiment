// <copyright file="ITransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for Transaction entity.
/// Transactions are owned by Account aggregate, but we need direct queries for calendar views
/// and write operations for transfers.
/// Composes <see cref="ITransactionQueryRepository"/>, <see cref="ITransactionImportRepository"/>,
/// and <see cref="ITransactionAnalyticsRepository"/> for full access.
/// </summary>
public interface ITransactionRepository :
    IReadRepository<Transaction>,
    IWriteRepository<Transaction>,
    ITransactionQueryRepository,
    ITransactionImportRepository,
    ITransactionAnalyticsRepository
{
    /// <summary>
    /// Deletes both legs of a transfer atomically using a database transaction.
    /// If only one leg exists (orphaned state), it is deleted with a warning logged.
    /// If neither leg exists, the method returns without error.
    /// </summary>
    /// <param name="transferId">The shared transfer identifier linking both transaction legs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteTransferAsync(Guid transferId, CancellationToken cancellationToken = default);
}

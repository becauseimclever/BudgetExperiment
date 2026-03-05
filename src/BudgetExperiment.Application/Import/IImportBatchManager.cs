// <copyright file="IImportBatchManager.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Manages import batch history and batch deletion operations.
/// </summary>
public interface IImportBatchManager
{
    /// <summary>
    /// Gets import history for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of import batches.</returns>
    Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all transactions from an import batch.
    /// </summary>
    /// <param name="batchId">The batch ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of transactions deleted.</returns>
    Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
}

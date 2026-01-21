// <copyright file="IImportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Service for importing transactions from CSV files.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Validates and previews import based on mapping configuration.
    /// Applies auto-categorization rules without saving.
    /// </summary>
    /// <param name="request">The preview request with rows and mappings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preview result with validation status and categorization.</returns>
    Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the import, creating transactions.
    /// </summary>
    /// <param name="request">The execution request with validated transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result with counts and created IDs.</returns>
    Task<ImportResult> ExecuteAsync(ImportExecuteRequest request, CancellationToken cancellationToken = default);

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

// <copyright file="ITransferService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Application service interface for account transfer operations.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Creates a new transfer between accounts.
    /// </summary>
    /// <param name="request">The transfer creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transfer response.</returns>
    Task<TransferResponse> CreateAsync(CreateTransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transfer by its identifier.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer response, or null if not found.</returns>
    Task<TransferResponse?> GetByIdAsync(Guid transferId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transfers, optionally filtered by account and/or date range.
    /// </summary>
    /// <param name="accountId">Optional account filter (returns transfers involving this account).</param>
    /// <param name="fromDate">Optional start date filter (inclusive).</param>
    /// <param name="toDate">Optional end date filter (inclusive).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged response of transfer list items.</returns>
    Task<TransferListPageResponse> ListAsync(
        Guid? accountId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transfer.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transfer response, or null if not found.</returns>
    Task<TransferResponse?> UpdateAsync(Guid transferId, UpdateTransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transfer and both associated transactions.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transfer was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid transferId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically deletes both legs of a transfer identified by <paramref name="transferId"/>.
    /// Delegates all-or-nothing semantics to the repository. Orphaned legs are cleaned up
    /// with a warning. Returns <c>false</c> immediately if no legs are found (not found case).
    /// </summary>
    /// <param name="transferId">The shared transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if at least one leg was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteTransferAsync(Guid transferId, CancellationToken cancellationToken = default);
}

// <copyright file="IReconciliationMatchActionHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Handles reconciliation match lifecycle actions (accept, reject, unlink, manual link).
/// </summary>
public interface IReconciliationMatchActionHandler
{
    /// <summary>
    /// Accepts a reconciliation match, linking the transaction to the recurring instance.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated match DTO, or null if not found.</returns>
    Task<ReconciliationMatchDto?> AcceptMatchAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a reconciliation match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated match DTO, or null if not found.</returns>
    Task<ReconciliationMatchDto?> RejectMatchAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk accepts multiple matches.
    /// </summary>
    /// <param name="request">The bulk action request containing match IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of accepted match DTOs.</returns>
    Task<IReadOnlyList<ReconciliationMatchDto>> BulkAcceptMatchesAsync(
        BulkMatchActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks a matched transaction, returning it and the recurring instance to unmatched state.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated match DTO, or null if not found.</returns>
    Task<ReconciliationMatchDto?> UnlinkMatchAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a manual match between a transaction and a recurring instance.
    /// </summary>
    /// <param name="request">The manual match request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created match DTO, or null if transaction or recurring not found.</returns>
    Task<ReconciliationMatchDto?> CreateManualMatchAsync(
        ManualMatchRequest request,
        CancellationToken cancellationToken = default);
}

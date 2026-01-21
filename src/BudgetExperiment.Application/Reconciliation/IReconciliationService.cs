// <copyright file="IReconciliationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Service for managing recurring transaction reconciliation.
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// Finds potential matches between imported transactions and recurring transaction instances.
    /// </summary>
    /// <param name="request">The find matches request with transaction IDs and date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matches found grouped by transaction.</returns>
    Task<FindMatchesResult> FindMatchesAsync(FindMatchesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending reconciliation matches awaiting user review.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending matches.</returns>
    Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reconciliation matches for a specific recurring transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches for the recurring transaction.</returns>
    Task<IReadOnlyList<ReconciliationMatchDto>> GetMatchesForRecurringTransactionAsync(
        Guid recurringTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a reconciliation match, linking the transaction to the recurring instance.
    /// </summary>
    /// <param name="matchId">The match ID to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated match.</returns>
    Task<ReconciliationMatchDto?> AcceptMatchAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a reconciliation match.
    /// </summary>
    /// <param name="matchId">The match ID to reject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated match.</returns>
    Task<ReconciliationMatchDto?> RejectMatchAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk accepts multiple matches.
    /// </summary>
    /// <param name="request">The bulk action request with match IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of accepted matches.</returns>
    Task<IReadOnlyList<ReconciliationMatchDto>> BulkAcceptMatchesAsync(
        BulkMatchActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reconciliation status for a specific year and month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reconciliation status for the period.</returns>
    Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a manual match between a transaction and a recurring instance.
    /// </summary>
    /// <param name="request">The manual match request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created match.</returns>
    Task<ReconciliationMatchDto?> CreateManualMatchAsync(
        ManualMatchRequest request,
        CancellationToken cancellationToken = default);
}

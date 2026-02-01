// <copyright file="IReconciliationApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service interface for reconciliation API operations.
/// </summary>
public interface IReconciliationApiService
{
    /// <summary>
    /// Gets the reconciliation status for a specific year and month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>Reconciliation status information.</returns>
    Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null);

    /// <summary>
    /// Gets pending matches awaiting review.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of pending matches.</returns>
    Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null);

    /// <summary>
    /// Finds matches for specific transactions.
    /// </summary>
    /// <param name="request">The find matches request.</param>
    /// <returns>Match results.</returns>
    Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request);

    /// <summary>
    /// Accepts a suggested match.
    /// </summary>
    /// <param name="matchId">The match ID to accept.</param>
    /// <returns>True if accepted successfully.</returns>
    Task<bool> AcceptMatchAsync(Guid matchId);

    /// <summary>
    /// Rejects a suggested match.
    /// </summary>
    /// <param name="matchId">The match ID to reject.</param>
    /// <returns>True if rejected successfully.</returns>
    Task<bool> RejectMatchAsync(Guid matchId);

    /// <summary>
    /// Bulk accepts multiple matches.
    /// </summary>
    /// <param name="matchIds">The match IDs to accept.</param>
    /// <returns>Number of matches accepted.</returns>
    Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds);

    /// <summary>
    /// Creates a manual match between a transaction and recurring instance.
    /// </summary>
    /// <param name="request">The manual match request.</param>
    /// <returns>The created match.</returns>
    Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request);

    /// <summary>
    /// Gets the current matching tolerances.
    /// </summary>
    /// <returns>The current tolerances.</returns>
    Task<MatchingTolerancesDto?> GetTolerancesAsync();

    /// <summary>
    /// Updates the matching tolerances.
    /// </summary>
    /// <param name="tolerances">The new tolerances.</param>
    /// <returns>True if updated successfully.</returns>
    Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances);

    /// <summary>
    /// Unlinks a matched transaction, returning it and the recurring instance to unmatched state.
    /// </summary>
    /// <param name="matchId">The match ID to unlink.</param>
    /// <returns>True if unlinked successfully.</returns>
    Task<bool> UnlinkMatchAsync(Guid matchId);

    /// <summary>
    /// Gets recurring instances that can be linked to a specific transaction.
    /// Returns instances within ±30 days of the transaction date.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>List of linkable recurring instances.</returns>
    Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(Guid transactionId);
}

// <copyright file="StubReconciliationApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IReconciliationApiService"/> for page-level bUnit tests.
/// </summary>
internal class StubReconciliationApiService : IReconciliationApiService
{
    /// <summary>
    /// Gets or sets the reconciliation status returned by <see cref="GetStatusAsync"/>.
    /// </summary>
    public ReconciliationStatusDto? Status { get; set; }

    /// <summary>
    /// Gets the list of pending matches returned by <see cref="GetPendingMatchesAsync"/>.
    /// </summary>
    public List<ReconciliationMatchDto> PendingMatches { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="AcceptMatchAsync"/> returns true.
    /// </summary>
    public bool AcceptMatchResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="RejectMatchAsync"/> returns true.
    /// </summary>
    public bool RejectMatchResult { get; set; }

    /// <summary>
    /// Gets or sets the number of matches accepted by <see cref="BulkAcceptMatchesAsync"/>.
    /// </summary>
    public int BulkAcceptResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="UnlinkMatchAsync"/> returns true.
    /// </summary>
    public bool UnlinkMatchResult { get; set; }

    /// <inheritdoc/>
    public Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null) =>
        Task.FromResult(this.Status);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null) =>
        Task.FromResult<IReadOnlyList<ReconciliationMatchDto>>(this.PendingMatches);

    /// <inheritdoc/>
    public Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request) =>
        Task.FromResult<FindMatchesResult?>(null);

    /// <inheritdoc/>
    public Task<bool> AcceptMatchAsync(Guid matchId) =>
        Task.FromResult(this.AcceptMatchResult);

    /// <inheritdoc/>
    public Task<bool> RejectMatchAsync(Guid matchId) =>
        Task.FromResult(this.RejectMatchResult);

    /// <inheritdoc/>
    public Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds) =>
        Task.FromResult(this.BulkAcceptResult);

    /// <inheritdoc/>
    public Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request) =>
        Task.FromResult<ReconciliationMatchDto?>(null);

    /// <inheritdoc/>
    public Task<MatchingTolerancesDto?> GetTolerancesAsync() =>
        Task.FromResult<MatchingTolerancesDto?>(null);

    /// <inheritdoc/>
    public Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances) =>
        Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> UnlinkMatchAsync(Guid matchId) =>
        Task.FromResult(this.UnlinkMatchResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(Guid transactionId) =>
        Task.FromResult<IReadOnlyList<LinkableInstanceDto>>([]);
}

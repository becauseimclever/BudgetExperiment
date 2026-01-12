// <copyright file="IPastDueService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for past-due recurring item operations.
/// </summary>
public interface IPastDueService
{
    /// <summary>
    /// Gets a summary of all past-due recurring items.
    /// </summary>
    /// <param name="accountId">Optional account ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of past-due items.</returns>
    Task<PastDueSummaryDto> GetPastDueItemsAsync(Guid? accountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Realizes multiple past-due items in batch.
    /// </summary>
    /// <param name="request">The batch realize request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results of the batch operation.</returns>
    Task<BatchRealizeResultDto> RealizeBatchAsync(BatchRealizeRequest request, CancellationToken cancellationToken = default);
}

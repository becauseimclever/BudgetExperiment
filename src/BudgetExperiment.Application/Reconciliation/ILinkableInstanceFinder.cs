// <copyright file="ILinkableInstanceFinder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Finds linkable recurring instances for a given transaction.
/// </summary>
public interface ILinkableInstanceFinder
{
    /// <summary>
    /// Gets linkable recurring instances for the specified transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of linkable instance DTOs.</returns>
    Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}

// <copyright file="IDismissedOutlierRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Reconciliation;

namespace BudgetExperiment.Domain.Repositories;

/// <summary>Repository interface for DismissedOutlier entity.</summary>
public interface IDismissedOutlierRepository : IReadRepository<DismissedOutlier>, IWriteRepository<DismissedOutlier>
{
    /// <summary>Checks whether the given transaction has been dismissed as an outlier.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if dismissed, false otherwise.</returns>
    Task<bool> IsDismissedAsync(Guid transactionId, CancellationToken ct);

    /// <summary>Gets all dismissed transaction IDs.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of dismissed transaction IDs.</returns>
    Task<IReadOnlyList<Guid>> GetDismissedTransactionIdsAsync(CancellationToken ct);
}

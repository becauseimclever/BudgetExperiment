// <copyright file="IReconciliationRecordRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="ReconciliationRecord"/> aggregate.
/// </summary>
public interface IReconciliationRecordRepository : IReadRepository<ReconciliationRecord>, IWriteRepository<ReconciliationRecord>
{
    /// <summary>
    /// Gets all reconciliation records for a given account, ordered by statement date descending.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Reconciliation records for the account.</returns>
    Task<IReadOnlyList<ReconciliationRecord>> GetByAccountAsync(Guid accountId, CancellationToken ct);

    /// <summary>
    /// Gets the most recent reconciliation record for a given account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest reconciliation record, or null if none exists.</returns>
    Task<ReconciliationRecord?> GetLatestByAccountAsync(Guid accountId, CancellationToken ct);
}

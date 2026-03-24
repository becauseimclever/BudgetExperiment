// <copyright file="IStatementBalanceRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="StatementBalance"/> aggregate.
/// </summary>
public interface IStatementBalanceRepository : IReadRepository<StatementBalance>, IWriteRepository<StatementBalance>
{
    /// <summary>
    /// Gets the active (non-completed) statement balance for a given account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active statement balance, or null if none exists.</returns>
    Task<StatementBalance?> GetActiveByAccountAsync(Guid accountId, CancellationToken ct);
}

// <copyright file="IAccountTransactionRangeRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Provides date-range access for account transactions.
/// </summary>
public interface IAccountTransactionRangeRepository
{
    /// <summary>
    /// Gets an account by ID including transactions within a date range.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account with transactions or null.</returns>
    Task<Account?> GetByIdWithTransactionsAsync(
        Guid id,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}

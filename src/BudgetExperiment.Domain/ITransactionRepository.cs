// <copyright file="ITransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for Transaction entity queries.
/// Transactions are owned by Account aggregate, but we need direct queries for calendar views.
/// </summary>
public interface ITransactionRepository : IReadRepository<Transaction>
{
    /// <summary>
    /// Gets transactions within a date range, optionally filtered by account.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions in the date range.</returns>
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily transaction totals for a month (for calendar summary view).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily totals as (date, total amount).</returns>
    Task<IReadOnlyList<DailyTotal>> GetDailyTotalsAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

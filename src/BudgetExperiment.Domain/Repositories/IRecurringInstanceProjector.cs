// <copyright file="IRecurringInstanceProjector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Projects recurring transaction instances for date ranges.
/// </summary>
public interface IRecurringInstanceProjector
{
    /// <summary>
    /// Gets projected recurring transaction instances grouped by date for a date range.
    /// </summary>
    /// <param name="recurringTransactions">The recurring transactions to project.</param>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping dates to lists of recurring instances for that date.</returns>
    Task<Dictionary<DateOnly, List<RecurringInstanceInfo>>> GetInstancesByDateRangeAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected recurring transaction instances for a specific date.
    /// </summary>
    /// <param name="recurringTransactions">The recurring transactions to project.</param>
    /// <param name="date">The date to get instances for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring instances for the specified date.</returns>
    Task<List<RecurringInstanceInfo>> GetInstancesForDateAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly date,
        CancellationToken cancellationToken = default);
}

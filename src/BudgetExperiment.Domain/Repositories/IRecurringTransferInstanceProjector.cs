// <copyright file="IRecurringTransferInstanceProjector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Projects recurring transfer instances for date ranges.
/// </summary>
public interface IRecurringTransferInstanceProjector
{
    /// <summary>
    /// Gets projected recurring transfer instances grouped by date for a date range.
    /// Creates entries for both source (negative) and destination (positive) sides of each transfer.
    /// </summary>
    /// <param name="recurringTransfers">The recurring transfers to project.</param>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="accountId">Optional account filter. When provided, only returns instances involving this account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping dates to lists of recurring transfer instances for that date.</returns>
    Task<Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>> GetInstancesByDateRangeAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected recurring transfer instances for a specific date.
    /// Creates entries for both source (negative) and destination (positive) sides of each transfer.
    /// </summary>
    /// <param name="recurringTransfers">The recurring transfers to project.</param>
    /// <param name="date">The date to get instances for.</param>
    /// <param name="accountId">Optional account filter. When provided, only returns instances involving this account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfer instances for the specified date.</returns>
    Task<List<RecurringTransferInstanceInfo>> GetInstancesForDateAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

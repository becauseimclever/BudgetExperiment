// <copyright file="IRecurringQueryService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service that coordinates recurring projection with realized transaction exclusion,
/// ensuring no double-counting of projected and realized recurring instances.
/// </summary>
public interface IRecurringQueryService
{
    /// <summary>
    /// Gets projected recurring transaction instances for a date range, excluding dates
    /// where transactions have already been realized from any of the provided recurring templates.
    /// </summary>
    /// <param name="recurringTransactions">The recurring transactions to project.</param>
    /// <param name="fromDate">Start of the date range (inclusive).</param>
    /// <param name="toDate">End of the date range (inclusive).</param>
    /// <param name="accountId">Optional account filter for realized transaction lookup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping dates to lists of projected (not yet realized) recurring instances.</returns>
    Task<Dictionary<DateOnly, List<RecurringInstanceInfoValue>>> GetProjectedInstancesAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

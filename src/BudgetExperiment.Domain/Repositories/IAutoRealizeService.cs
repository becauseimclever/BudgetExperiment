// <copyright file="IAutoRealizeService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Service for auto-realizing past-due recurring items.
/// </summary>
public interface IAutoRealizeService
{
    /// <summary>
    /// Auto-realizes past-due recurring items if the auto-realize setting is enabled.
    /// Creates actual transactions for recurring transactions and recurring transfers
    /// that occurred between the lookback date and yesterday.
    /// </summary>
    /// <param name="today">Today's date (used to determine "yesterday" and lookback range).</param>
    /// <param name="accountId">Optional account filter. When provided, only realizes items involving this account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of items that were realized.</returns>
    Task<int> AutoRealizePastDueItemsIfEnabledAsync(
        DateOnly today,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}

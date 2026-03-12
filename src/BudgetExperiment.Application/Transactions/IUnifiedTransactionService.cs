// <copyright file="IUnifiedTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Transactions;

/// <summary>
/// Service interface for the unified transaction list.
/// </summary>
public interface IUnifiedTransactionService
{
    /// <summary>
    /// Gets a paged, filtered, and sorted list of all transactions across all accounts.
    /// </summary>
    /// <param name="filter">The filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing unified transaction items, summary, and optional balance info.</returns>
    Task<UnifiedTransactionPageDto> GetPagedAsync(
        UnifiedTransactionFilterDto filter,
        CancellationToken cancellationToken = default);
}

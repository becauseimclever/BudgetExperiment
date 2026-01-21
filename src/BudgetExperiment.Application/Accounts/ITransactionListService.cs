// <copyright file="ITransactionListService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Service interface for building account transaction lists.
/// </summary>
public interface ITransactionListService
{
    /// <summary>
    /// Gets a pre-merged transaction list for an account over a date range.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="includeRecurring">Whether to include recurring transaction instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction list DTO with pre-computed summaries.</returns>
    Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true,
        CancellationToken cancellationToken = default);
}

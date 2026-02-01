// <copyright file="IUncategorizedTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service interface for managing uncategorized transactions.
/// </summary>
public interface IUncategorizedTransactionService
{
    /// <summary>
    /// Gets a paged list of uncategorized transactions with optional filtering and sorting.
    /// </summary>
    /// <param name="filter">The filter, sort, and paging parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing transaction DTOs and total count.</returns>
    Task<UncategorizedTransactionPageDto> GetPagedAsync(
        UncategorizedTransactionFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk categorizes multiple transactions with the specified category.
    /// </summary>
    /// <param name="request">The bulk categorize request containing transaction IDs and category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response indicating success/failure counts and any errors.</returns>
    Task<BulkCategorizeResponse> BulkCategorizeAsync(
        BulkCategorizeRequest request,
        CancellationToken cancellationToken = default);
}

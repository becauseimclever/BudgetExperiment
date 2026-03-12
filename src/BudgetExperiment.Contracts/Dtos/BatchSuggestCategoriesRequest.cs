// <copyright file="BatchSuggestCategoriesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to get category suggestions for a batch of transactions.
/// </summary>
public sealed class BatchSuggestCategoriesRequest
{
    /// <summary>
    /// Gets or sets the transaction IDs to get suggestions for.
    /// </summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];
}

// <copyright file="UncategorizedTransactionPageDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Paged response for uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionPageDto
{
    /// <summary>Gets or sets the transaction items for the current page.</summary>
    public IReadOnlyList<TransactionDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total count of matching transactions.</summary>
    public int TotalCount
    {
        get; set;
    }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page
    {
        get; set;
    }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize
    {
        get; set;
    }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

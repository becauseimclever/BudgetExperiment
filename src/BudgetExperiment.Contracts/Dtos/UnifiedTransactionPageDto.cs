// <copyright file="UnifiedTransactionPageDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Paged response for the unified transaction list endpoint.
/// </summary>
public sealed class UnifiedTransactionPageDto
{
    /// <summary>Gets or sets the transaction items for the current page.</summary>
    public IReadOnlyList<UnifiedTransactionItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total count of matching transactions across all pages.</summary>
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

    /// <summary>Gets or sets the summary statistics for the filtered result set.</summary>
    public UnifiedTransactionSummaryDto? Summary
    {
        get; set;
    }

    /// <summary>Gets or sets the account balance info (only populated when filtered to a single account).</summary>
    public AccountBalanceInfoDto? BalanceInfo
    {
        get; set;
    }
}

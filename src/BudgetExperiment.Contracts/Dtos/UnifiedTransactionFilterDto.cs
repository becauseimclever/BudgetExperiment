// <copyright file="UnifiedTransactionFilterDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Filter, sort, and pagination parameters for the unified transaction list endpoint.
/// </summary>
public sealed class UnifiedTransactionFilterDto
{
    /// <summary>Gets or sets the optional account filter.</summary>
    public Guid? AccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the optional category filter.</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether to show only uncategorized transactions.</summary>
    public bool? Uncategorized
    {
        get; set;
    }

    /// <summary>Gets or sets the optional start date filter (inclusive).</summary>
    public DateOnly? StartDate
    {
        get; set;
    }

    /// <summary>Gets or sets the optional end date filter (inclusive).</summary>
    public DateOnly? EndDate
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional description search (contains, case-insensitive).
    /// In encrypted mode this filter is applied after materialization to preserve semantic correctness.
    /// </summary>
    public string? Description
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional minimum amount filter (absolute value).
    /// In encrypted mode this filter is applied after materialization to preserve semantic correctness.
    /// </summary>
    public decimal? MinAmount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional maximum amount filter (absolute value).
    /// In encrypted mode this filter is applied after materialization to preserve semantic correctness.
    /// </summary>
    public decimal? MaxAmount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the sort field: "date", "description", "amount", "category", "account" (default: "date").
    /// In encrypted mode, sorting by "description", "amount", and "account" is applied in-memory.
    /// </summary>
    public string SortBy { get; set; } = "date";

    /// <summary>Gets or sets a value indicating whether to sort descending (default: true).</summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>Gets or sets the page number (1-based, default: 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size (default: 50, max: 100).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Gets or sets the optional Kakeibo category filter (enum name, e.g. "Wants"). Null returns all transactions.</summary>
    public string? KakeiboCategory
    {
        get; set;
    }
}

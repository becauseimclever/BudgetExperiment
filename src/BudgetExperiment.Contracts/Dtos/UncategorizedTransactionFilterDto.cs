// <copyright file="UncategorizedTransactionFilterDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Filter parameters for querying uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionFilterDto
{
    /// <summary>Gets or sets the optional start date filter (inclusive).</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Gets or sets the optional end date filter (inclusive).</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the optional minimum amount filter (absolute value).</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Gets or sets the optional maximum amount filter (absolute value).</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>Gets or sets the optional description contains filter (case-insensitive).</summary>
    public string? DescriptionContains { get; set; }

    /// <summary>Gets or sets the optional account filter.</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the sort field: "Date", "Amount", or "Description".</summary>
    public string SortBy { get; set; } = "Date";

    /// <summary>Gets or sets a value indicating whether to sort descending (default: true).</summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>Gets or sets the page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size (default: 50, max: 100).</summary>
    public int PageSize { get; set; } = 50;
}

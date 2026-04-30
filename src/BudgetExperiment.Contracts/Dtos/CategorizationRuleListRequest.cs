// <copyright file="CategorizationRuleListRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Paginated request parameters for listing categorization rules.
/// </summary>
public sealed class CategorizationRuleListRequest
{
    /// <summary>Gets or sets the page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets the search text to filter by rule name or pattern.
    /// In encrypted mode, pattern matching is applied in-memory after base filters.
    /// </summary>
    public string? Search
    {
        get; set;
    }

    /// <summary>Gets or sets the category ID to filter by.</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets the status filter: "active", "inactive", or null for all.</summary>
    public string? Status
    {
        get; set;
    }

    /// <summary>Gets or sets the sort field: "priority", "name", "category", "createdAt".</summary>
    public string? SortBy
    {
        get; set;
    }

    /// <summary>Gets or sets the sort direction: "asc" or "desc".</summary>
    public string? SortDirection
    {
        get; set;
    }
}

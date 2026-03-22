// <copyright file="CategorizationRulePageResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Paged response for categorization rules listing.
/// </summary>
public sealed class CategorizationRulePageResponse
{
    /// <summary>Gets or sets the rule items for the current page.</summary>
    public IReadOnlyList<CategorizationRuleDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total count of matching rules.</summary>
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
    public int TotalPages => this.PageSize > 0 ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 0;
}

// <copyright file="BatchSuggestCategoriesResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response containing category suggestions for a batch of transactions.
/// </summary>
public sealed class BatchSuggestCategoriesResponse
{
    /// <summary>
    /// Gets or sets the suggestions keyed by transaction ID.
    /// Only includes transactions that had a matching rule.
    /// </summary>
    public Dictionary<Guid, InlineCategorySuggestionDto> Suggestions { get; set; } = new();
}

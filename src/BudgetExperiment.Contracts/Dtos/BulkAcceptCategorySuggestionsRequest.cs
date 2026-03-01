// <copyright file="BulkAcceptCategorySuggestionsRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request for bulk accepting category suggestions.
/// </summary>
public sealed record BulkAcceptCategorySuggestionsRequest
{
    /// <summary>
    /// Gets the list of suggestion IDs to accept.
    /// </summary>
    public required IReadOnlyList<Guid> SuggestionIds { get; init; }
}

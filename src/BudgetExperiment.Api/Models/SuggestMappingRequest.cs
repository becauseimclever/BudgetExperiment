// <copyright file="SuggestMappingRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Models;

/// <summary>
/// Request for suggesting a mapping based on headers.
/// </summary>
public sealed record SuggestMappingRequest
{
    /// <summary>
    /// Gets the CSV headers to match against existing mappings.
    /// </summary>
    public IReadOnlyList<string> Headers { get; init; } = [];
}

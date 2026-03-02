// <copyright file="BulkMatchActionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to bulk accept or reject matches.
/// </summary>
public sealed record BulkMatchActionRequest
{
    /// <summary>
    /// Gets the match IDs to process.
    /// </summary>
    public IReadOnlyList<Guid> MatchIds { get; init; } = [];
}

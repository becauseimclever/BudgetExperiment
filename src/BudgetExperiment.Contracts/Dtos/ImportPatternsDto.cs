// <copyright file="ImportPatternsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for managing import patterns on a recurring transaction.
/// </summary>
public sealed record ImportPatternsDto
{
    /// <summary>Gets the list of import patterns.</summary>
    public IReadOnlyList<string> Patterns { get; init; } = [];
}

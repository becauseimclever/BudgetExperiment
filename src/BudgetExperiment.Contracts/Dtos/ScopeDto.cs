// <copyright file="ScopeDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for the current user's scope selection.
/// </summary>
public sealed class ScopeDto
{
    /// <summary>Gets or sets the current scope ("Shared", "Personal", or null for All).</summary>
    public string? Scope
    {
        get; set;
    }
}

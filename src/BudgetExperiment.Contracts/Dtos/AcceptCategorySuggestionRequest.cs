// <copyright file="AcceptCategorySuggestionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to accept a category suggestion with optional customizations.
/// </summary>
public sealed record AcceptCategorySuggestionRequest
{
    /// <summary>
    /// Gets or sets the custom name to use instead of the suggested name.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// Gets or sets the custom icon to use.
    /// </summary>
    public string? CustomIcon { get; init; }

    /// <summary>
    /// Gets or sets the custom color to use.
    /// </summary>
    public string? CustomColor { get; init; }

    /// <summary>
    /// Gets or sets whether to auto-create categorization rules.
    /// </summary>
    public bool CreateRules { get; init; } = true;
}

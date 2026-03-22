// <copyright file="DismissSuggestionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for dismissing a suggestion.
/// </summary>
public sealed class DismissSuggestionRequest
{
    /// <summary>
    /// Gets or sets the optional reason for dismissal.
    /// </summary>
    public string? Reason
    {
        get; set;
    }
}

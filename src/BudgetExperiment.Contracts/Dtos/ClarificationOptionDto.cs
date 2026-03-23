// <copyright file="ClarificationOptionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a clarification option.
/// </summary>
public sealed class ClarificationOptionDto
{
    /// <summary>
    /// Gets or sets the display label for the option.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to use when selected.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional entity identifier.
    /// </summary>
    public Guid? EntityId
    {
        get; set;
    }
}

// <copyright file="ClarificationOption.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// An option for clarification selection.
/// </summary>
public sealed record ClarificationOption
{
    /// <summary>
    /// Gets the display label for the option.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the value to use when selected.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional entity identifier (for account/category selection).
    /// </summary>
    public Guid? EntityId { get; init; }
}

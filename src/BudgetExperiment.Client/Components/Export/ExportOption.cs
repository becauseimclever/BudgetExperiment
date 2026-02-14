// <copyright file="ExportOption.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Export;

/// <summary>
/// Represents a single export option.
/// </summary>
public sealed record ExportOption
{
    /// <summary>
    /// Gets the label shown for the option.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the target URL for the export.
    /// </summary>
    public required string Href { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the option is disabled.
    /// </summary>
    public bool Disabled { get; init; }
}

// <copyright file="ImportLocationPreview.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Preview of a parsed location from a transaction description during import.
/// </summary>
public sealed record ImportLocationPreview
{
    /// <summary>
    /// Gets the parsed city name.
    /// </summary>
    public string? City
    {
        get; init;
    }

    /// <summary>
    /// Gets the parsed state or region abbreviation.
    /// </summary>
    public string? StateOrRegion
    {
        get; init;
    }

    /// <summary>
    /// Gets the parsed country code (ISO 3166-1 alpha-2).
    /// </summary>
    public string? Country
    {
        get; init;
    }

    /// <summary>
    /// Gets the parsed postal code.
    /// </summary>
    public string? PostalCode
    {
        get; init;
    }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0) of the parsed location.
    /// </summary>
    public decimal Confidence
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether this parsed location is accepted for import.
    /// Defaults to <see langword="true"/>. User can reject individual locations in the preview.
    /// </summary>
    public bool IsAccepted { get; init; } = true;
}

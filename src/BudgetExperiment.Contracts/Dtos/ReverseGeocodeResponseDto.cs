// <copyright file="ReverseGeocodeResponseDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO containing the result of a reverse geocoding lookup.
/// </summary>
public sealed class ReverseGeocodeResponseDto
{
    /// <summary>Gets or sets the city name.</summary>
    public string? City
    {
        get; set;
    }

    /// <summary>Gets or sets the state or region name.</summary>
    public string? StateOrRegion
    {
        get; set;
    }

    /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
    public string? Country
    {
        get; set;
    }

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode
    {
        get; set;
    }

    /// <summary>Gets or sets the fully formatted address string.</summary>
    public string? FormattedAddress
    {
        get; set;
    }
}

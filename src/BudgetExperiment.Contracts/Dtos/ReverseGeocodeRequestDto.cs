// <copyright file="ReverseGeocodeRequestDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for requesting reverse geocoding of GPS coordinates.
/// </summary>
public sealed class ReverseGeocodeRequestDto
{
    /// <summary>Gets or sets the latitude coordinate (-90 to 90).</summary>
    public decimal Latitude { get; set; }

    /// <summary>Gets or sets the longitude coordinate (-180 to 180).</summary>
    public decimal Longitude { get; set; }
}

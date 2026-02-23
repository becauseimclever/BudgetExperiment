// <copyright file="TransactionLocationUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating a transaction's location data.
/// </summary>
public sealed class TransactionLocationUpdateDto
{
    /// <summary>Gets or sets the latitude coordinate.</summary>
    public decimal? Latitude { get; set; }

    /// <summary>Gets or sets the longitude coordinate.</summary>
    public decimal? Longitude { get; set; }

    /// <summary>Gets or sets the city name.</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the state or region name.</summary>
    public string? StateOrRegion { get; set; }

    /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
}

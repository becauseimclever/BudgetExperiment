// <copyright file="TransactionLocation.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Immutable value object representing the geographic location of a transaction.
/// </summary>
public sealed record TransactionLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionLocation"/> class.
    /// </summary>
    private TransactionLocation()
    {
    }

    /// <summary>
    /// Gets the geographic coordinates (latitude/longitude), if available.
    /// </summary>
    public GeoCoordinate? Coordinates { get; init; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Gets the state or region name.
    /// </summary>
    public string? StateOrRegion { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets the source that determined this location.
    /// </summary>
    public LocationSource Source { get; init; }

    /// <summary>
    /// Creates a validated <see cref="TransactionLocation"/> with normalized fields.
    /// </summary>
    /// <param name="city">City name.</param>
    /// <param name="stateOrRegion">State or region name.</param>
    /// <param name="country">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="postalCode">Postal code.</param>
    /// <param name="coordinates">Geographic coordinates.</param>
    /// <param name="source">How the location was determined.</param>
    /// <returns>A new <see cref="TransactionLocation"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when no meaningful location field is provided.</exception>
    public static TransactionLocation Create(
        string? city,
        string? stateOrRegion,
        string? country,
        string? postalCode,
        GeoCoordinate? coordinates,
        LocationSource source)
    {
        if (city is null && stateOrRegion is null && country is null && coordinates is null)
        {
            throw new DomainException("At least one location field must be provided.");
        }

        return new TransactionLocation
        {
            Coordinates = coordinates,
            City = city?.Trim(),
            StateOrRegion = stateOrRegion?.Trim(),
            Country = country?.Trim().ToUpperInvariant(),
            PostalCode = postalCode?.Trim(),
            Source = source,
        };
    }

    /// <summary>
    /// Creates a location from parsed transaction description data (assumed US).
    /// </summary>
    /// <param name="city">Parsed city name.</param>
    /// <param name="stateOrRegion">Parsed state abbreviation.</param>
    /// <returns>A new <see cref="TransactionLocation"/> with <see cref="LocationSource.Parsed"/> source.</returns>
    public static TransactionLocation CreateFromParsed(string city, string stateOrRegion)
        => Create(city, stateOrRegion, "US", null, null, LocationSource.Parsed);

    /// <summary>
    /// Creates a location from GPS coordinates.
    /// </summary>
    /// <param name="coordinates">GPS coordinates.</param>
    /// <returns>A new <see cref="TransactionLocation"/> with <see cref="LocationSource.Gps"/> source.</returns>
    public static TransactionLocation CreateFromGps(GeoCoordinate coordinates)
        => Create(null, null, null, null, coordinates, LocationSource.Gps);
}

// <copyright file="GeoCoordinateValue.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Immutable geographic coordinate value object (latitude/longitude, 6 decimal precision).
/// </summary>
public sealed record GeoCoordinateValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeoCoordinateValue"/> class.
    /// </summary>
    /// <param name="latitude">Latitude value.</param>
    /// <param name="longitude">Longitude value.</param>
    private GeoCoordinateValue(decimal latitude, decimal longitude)
    {
        this.Latitude = latitude;
        this.Longitude = longitude;
    }

    /// <summary>
    /// Gets the latitude (-90 to 90).
    /// </summary>
    public decimal Latitude
    {
        get; init;
    }

    /// <summary>
    /// Gets the longitude (-180 to 180).
    /// </summary>
    public decimal Longitude
    {
        get; init;
    }

    /// <summary>
    /// Creates a validated <see cref="GeoCoordinateValue"/> with 6 decimal place precision.
    /// </summary>
    /// <param name="latitude">Latitude (-90 to 90).</param>
    /// <param name="longitude">Longitude (-180 to 180).</param>
    /// <returns>A new <see cref="GeoCoordinateValue"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when coordinates are out of range.</exception>
    public static GeoCoordinateValue Create(decimal latitude, decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        if (longitude < -180m || longitude > 180m)
        {
            throw new DomainException("Longitude must be between -180 and 180.");
        }

        return new GeoCoordinateValue(
            decimal.Round(latitude, 6, MidpointRounding.AwayFromZero),
            decimal.Round(longitude, 6, MidpointRounding.AwayFromZero));
    }
}

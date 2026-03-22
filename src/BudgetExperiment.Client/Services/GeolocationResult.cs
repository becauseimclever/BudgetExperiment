// <copyright file="GeolocationResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Result of a geolocation request.
/// </summary>
public sealed class GeolocationResult
{
    private GeolocationResult(bool isSuccess, decimal latitude, decimal longitude, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Latitude = latitude;
        Longitude = longitude;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the position was obtained successfully.</summary>
    public bool IsSuccess
    {
        get;
    }

    /// <summary>Gets the latitude coordinate.</summary>
    public decimal Latitude
    {
        get;
    }

    /// <summary>Gets the longitude coordinate.</summary>
    public decimal Longitude
    {
        get;
    }

    /// <summary>Gets the error message if the request failed.</summary>
    public string? ErrorMessage
    {
        get;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    /// <returns>A successful <see cref="GeolocationResult"/>.</returns>
    public static GeolocationResult Success(decimal latitude, decimal longitude) =>
        new(true, latitude, longitude, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error description.</param>
    /// <returns>A failed <see cref="GeolocationResult"/>.</returns>
    public static GeolocationResult Failure(string errorMessage) =>
        new(false, 0, 0, errorMessage);
}

// <copyright file="IGeocodingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Location;

/// <summary>
/// Converts GPS coordinates to human-readable address components via reverse geocoding.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Performs reverse geocoding to resolve latitude/longitude to an address.
    /// </summary>
    /// <param name="latitude">The latitude coordinate (-90 to 90).</param>
    /// <param name="longitude">The longitude coordinate (-180 to 180).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ReverseGeocodeResponseDto"/> if resolved; otherwise <see langword="null"/>.</returns>
    Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}

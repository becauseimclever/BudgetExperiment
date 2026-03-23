// <copyright file="NominatimGeocodingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using BudgetExperiment.Application.Location;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.Geocoding;

/// <summary>
/// Reverse geocoding service backed by the OpenStreetMap Nominatim API.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org";

    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NominatimGeocodingService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);
            var url = $"{NominatimBaseUrl}/reverse?lat={lat}&lon={lon}&format=json&addressdetails=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "BudgetExperiment/1.0");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim returned status {StatusCode} for ({Lat}, {Lon})", response.StatusCode, latitude, longitude);
                return null;
            }

            var nominatimResult = await response.Content.ReadFromJsonAsync<NominatimResponse>(cancellationToken: cancellationToken);

            if (nominatimResult is null)
            {
                return null;
            }

            var address = nominatimResult.Address;

            return new ReverseGeocodeResponseDto
            {
                City = address?.City ?? address?.Town ?? address?.Village,
                StateOrRegion = address?.State,
                Country = address?.CountryCode?.ToUpperInvariant(),
                PostalCode = address?.PostCode,
                FormattedAddress = nominatimResult.DisplayName,
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to call Nominatim for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Nominatim request timed out for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    /// <summary>
    /// Nominatim API response shape.
    /// </summary>
    private sealed class NominatimResponse
    {
        /// <summary>Gets or sets the display name.</summary>
        [JsonPropertyName("display_name")]
        public string? DisplayName
        {
            get; set;
        }

        /// <summary>Gets or sets the address details.</summary>
        [JsonPropertyName("address")]
        public NominatimAddress? Address
        {
            get; set;
        }
    }

    /// <summary>
    /// Nominatim address breakdown.
    /// </summary>
    private sealed class NominatimAddress
    {
        /// <summary>Gets or sets the city.</summary>
        [JsonPropertyName("city")]
        public string? City
        {
            get; set;
        }

        /// <summary>Gets or sets the town (fallback for city).</summary>
        [JsonPropertyName("town")]
        public string? Town
        {
            get; set;
        }

        /// <summary>Gets or sets the village (fallback for city).</summary>
        [JsonPropertyName("village")]
        public string? Village
        {
            get; set;
        }

        /// <summary>Gets or sets the state.</summary>
        [JsonPropertyName("state")]
        public string? State
        {
            get; set;
        }

        /// <summary>Gets or sets the ISO country code.</summary>
        [JsonPropertyName("country_code")]
        public string? CountryCode
        {
            get; set;
        }

        /// <summary>Gets or sets the postal code.</summary>
        [JsonPropertyName("postcode")]
        public string? PostCode
        {
            get; set;
        }
    }
}

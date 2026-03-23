// <copyright file="GeocodingControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Geocoding API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class GeocodingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocodingControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public GeocodingControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/geocoding/reverse with valid coordinates returns 200.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Post_ValidCoordinates_Returns200()
    {
        // Arrange
        var request = new ReverseGeocodeRequestDto
        {
            Latitude = 47.6062m,
            Longitude = -122.3321m,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/geocoding/reverse", request);

        // Assert — the service may return null (no real Nominatim in test), so we accept 200 or 204
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {response.StatusCode}");
    }

    /// <summary>
    /// POST /api/v1/geocoding/reverse with out-of-range latitude returns 422.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Post_InvalidLatitude_Returns422()
    {
        // Arrange
        var request = new ReverseGeocodeRequestDto
        {
            Latitude = 91m,
            Longitude = -122.3321m,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/geocoding/reverse", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/geocoding/reverse with out-of-range longitude returns 422.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Post_InvalidLongitude_Returns422()
    {
        // Arrange
        var request = new ReverseGeocodeRequestDto
        {
            Latitude = 47.6062m,
            Longitude = 181m,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/geocoding/reverse", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

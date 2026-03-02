// <copyright file="NominatimGeocodingServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

using BudgetExperiment.Application.Location;
using BudgetExperiment.Infrastructure.ExternalServices.Geocoding;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="NominatimGeocodingService"/> using a fake HTTP handler.
/// </summary>
public sealed class NominatimGeocodingServiceTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="NominatimGeocodingServiceTests"/> class.
    /// </summary>
    public NominatimGeocodingServiceTests()
    {
        _handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    /// <summary>
    /// Valid coordinates should return a populated address.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocode_ValidCoordinates_ReturnsAddress()
    {
        // Arrange
        _handler.ResponseContent = """
        {
            "address": {
                "city": "Seattle",
                "state": "Washington",
                "country_code": "us",
                "postcode": "98101"
            },
            "display_name": "Seattle, King County, Washington, 98101, United States"
        }
        """;
        _handler.StatusCode = HttpStatusCode.OK;

        var service = CreateService();

        // Act
        var result = await service.ReverseGeocodeAsync(47.6062m, -122.3321m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Seattle", result.City);
        Assert.Equal("Washington", result.StateOrRegion);
        Assert.Equal("US", result.Country);
        Assert.Equal("98101", result.PostalCode);
        Assert.Equal("Seattle, King County, Washington, 98101, United States", result.FormattedAddress);
    }

    /// <summary>
    /// When the API returns an error status, the service should return null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocode_ApiError_ReturnsNull()
    {
        // Arrange
        _handler.ResponseContent = "Internal Server Error";
        _handler.StatusCode = HttpStatusCode.InternalServerError;

        var service = CreateService();

        // Act
        var result = await service.ReverseGeocodeAsync(47.6062m, -122.3321m);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// When the API returns empty address data, the service should still return non-null with available fields.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocode_EmptyAddress_ReturnsResponseWithNulls()
    {
        // Arrange
        _handler.ResponseContent = """
        {
            "address": {},
            "display_name": "Unknown Location"
        }
        """;
        _handler.StatusCode = HttpStatusCode.OK;

        var service = CreateService();

        // Act
        var result = await service.ReverseGeocodeAsync(0m, 0m);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.City);
        Assert.Null(result.StateOrRegion);
        Assert.Equal("Unknown Location", result.FormattedAddress);
    }

    /// <summary>
    /// Nominatim uses 'town' or 'village' when 'city' is absent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocode_TownFallback_ReturnsTownAsCity()
    {
        // Arrange
        _handler.ResponseContent = """
        {
            "address": {
                "town": "Redmond",
                "state": "Washington",
                "country_code": "us",
                "postcode": "98052"
            },
            "display_name": "Redmond, King County, Washington, 98052, United States"
        }
        """;
        _handler.StatusCode = HttpStatusCode.OK;

        var service = CreateService();

        // Act
        var result = await service.ReverseGeocodeAsync(47.6740m, -122.1215m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Redmond", result.City);
    }

    /// <summary>
    /// The correct query string is sent to Nominatim.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocode_SendsCorrectRequestUrl()
    {
        // Arrange
        _handler.ResponseContent = """{ "address": {}, "display_name": "" }""";
        _handler.StatusCode = HttpStatusCode.OK;

        var service = CreateService();

        // Act
        await service.ReverseGeocodeAsync(47.6062m, -122.3321m);

        // Assert
        Assert.NotNull(_handler.LastRequestUri);
        var query = _handler.LastRequestUri.Query;
        Assert.Contains("lat=47.6062", query);
        Assert.Contains("lon=-122.3321", query);
        Assert.Contains("format=json", query);
        Assert.Contains("addressdetails=1", query);
    }

    private NominatimGeocodingService CreateService()
    {
        return new NominatimGeocodingService(_httpClient, NullLogger<NominatimGeocodingService>.Instance);
    }
}

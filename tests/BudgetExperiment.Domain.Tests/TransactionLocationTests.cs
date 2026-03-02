// <copyright file="TransactionLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="TransactionLocationValue"/> value object.
/// </summary>
public class TransactionLocationTests
{
    [Fact]
    public void Create_WithCity_ReturnsInstance()
    {
        // Arrange & Act
        var location = TransactionLocationValue.Create(
            city: "Seattle",
            stateOrRegion: "WA",
            country: "US",
            postalCode: "98101",
            coordinates: null,
            source: LocationSource.Manual);

        // Assert
        Assert.Equal("Seattle", location.City);
        Assert.Equal("WA", location.StateOrRegion);
        Assert.Equal("US", location.Country);
        Assert.Equal("98101", location.PostalCode);
        Assert.Null(location.Coordinates);
        Assert.Equal(LocationSource.Manual, location.Source);
    }

    [Fact]
    public void Create_WithAllFieldsNull_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            TransactionLocationValue.Create(null, null, null, null, null, LocationSource.Manual));
        Assert.Contains("At least one location field", ex.Message);
    }

    [Fact]
    public void Create_NormalizesCountryToUpperCase()
    {
        // Arrange & Act
        var location = TransactionLocationValue.Create(
            city: "Berlin",
            stateOrRegion: null,
            country: "de",
            postalCode: null,
            coordinates: null,
            source: LocationSource.Manual);

        // Assert
        Assert.Equal("DE", location.Country);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Arrange & Act
        var location = TransactionLocationValue.Create(
            city: "  Seattle  ",
            stateOrRegion: " WA ",
            country: " us ",
            postalCode: " 98101 ",
            coordinates: null,
            source: LocationSource.Manual);

        // Assert
        Assert.Equal("Seattle", location.City);
        Assert.Equal("WA", location.StateOrRegion);
        Assert.Equal("US", location.Country);
        Assert.Equal("98101", location.PostalCode);
    }

    [Fact]
    public void Create_WithOnlyCoordinates_ReturnsInstance()
    {
        // Arrange
        var coords = GeoCoordinateValue.Create(47.6062m, -122.3321m);

        // Act
        var location = TransactionLocationValue.Create(
            city: null,
            stateOrRegion: null,
            country: null,
            postalCode: null,
            coordinates: coords,
            source: LocationSource.Gps);

        // Assert
        Assert.Equal(coords, location.Coordinates);
        Assert.Null(location.City);
        Assert.Equal(LocationSource.Gps, location.Source);
    }

    [Fact]
    public void CreateFromParsed_SetsSourceToParsed()
    {
        // Arrange & Act
        var location = TransactionLocationValue.CreateFromParsed("Seattle", "WA");

        // Assert
        Assert.Equal("Seattle", location.City);
        Assert.Equal("WA", location.StateOrRegion);
        Assert.Equal("US", location.Country);
        Assert.Equal(LocationSource.Parsed, location.Source);
        Assert.Null(location.Coordinates);
    }

    [Fact]
    public void CreateFromGps_SetsSourceToGps()
    {
        // Arrange
        var coords = GeoCoordinateValue.Create(47.6062m, -122.3321m);

        // Act
        var location = TransactionLocationValue.CreateFromGps(coords);

        // Assert
        Assert.Equal(coords, location.Coordinates);
        Assert.Equal(LocationSource.Gps, location.Source);
        Assert.Null(location.City);
        Assert.Null(location.StateOrRegion);
    }

    [Fact]
    public void Create_WithPostalCodeOnly_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        // PostalCode alone is not sufficient — need city, stateOrRegion, country, or coordinates
        var ex = Assert.Throws<DomainException>(() =>
            TransactionLocationValue.Create(null, null, null, "98101", null, LocationSource.Manual));
        Assert.Contains("At least one location field", ex.Message);
    }
}

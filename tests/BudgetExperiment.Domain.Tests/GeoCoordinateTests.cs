// <copyright file="GeoCoordinateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="GeoCoordinate"/> value object.
/// </summary>
public class GeoCoordinateTests
{
    [Fact]
    public void Create_WithValidCoordinates_ReturnsInstance()
    {
        // Arrange & Act
        var coord = GeoCoordinate.Create(40.7128m, -74.0060m);

        // Assert
        Assert.Equal(40.7128m, coord.Latitude);
        Assert.Equal(-74.006m, coord.Longitude);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    [InlineData(-91)]
    [InlineData(100)]
    public void Create_WithLatitudeOutOfRange_ThrowsDomainException(double latitude)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => GeoCoordinate.Create((decimal)latitude, 0m));
        Assert.Contains("Latitude", ex.Message);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    [InlineData(-200)]
    [InlineData(200)]
    public void Create_WithLongitudeOutOfRange_ThrowsDomainException(double longitude)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => GeoCoordinate.Create(0m, (decimal)longitude));
        Assert.Contains("Longitude", ex.Message);
    }

    [Fact]
    public void Create_RoundsTo6DecimalPlaces()
    {
        // Arrange & Act
        var coord = GeoCoordinate.Create(40.71284567m, -74.00604321m);

        // Assert
        Assert.Equal(40.712846m, coord.Latitude);
        Assert.Equal(-74.006043m, coord.Longitude);
    }

    [Fact]
    public void Create_WithBoundaryValues_ReturnsInstance()
    {
        // Arrange & Act
        var min = GeoCoordinate.Create(-90m, -180m);
        var max = GeoCoordinate.Create(90m, 180m);

        // Assert
        Assert.Equal(-90m, min.Latitude);
        Assert.Equal(-180m, min.Longitude);
        Assert.Equal(90m, max.Latitude);
        Assert.Equal(180m, max.Longitude);
    }

    [Fact]
    public void EqualityByValue_SameCoordinates_AreEqual()
    {
        // Arrange
        var coord1 = GeoCoordinate.Create(40.7128m, -74.0060m);
        var coord2 = GeoCoordinate.Create(40.7128m, -74.0060m);

        // Assert
        Assert.Equal(coord1, coord2);
    }

    [Fact]
    public void EqualityByValue_DifferentCoordinates_AreNotEqual()
    {
        // Arrange
        var coord1 = GeoCoordinate.Create(40.7128m, -74.0060m);
        var coord2 = GeoCoordinate.Create(34.0522m, -118.2437m);

        // Assert
        Assert.NotEqual(coord1, coord2);
    }
}

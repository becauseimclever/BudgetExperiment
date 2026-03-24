// <copyright file="GeolocationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GeolocationService"/>.
/// </summary>
public class GeolocationServiceTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationServiceTests"/> class.
    /// </summary>
    public GeolocationServiceTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Strict;
    }

    /// <summary>
    /// GetCurrentPosition invokes the JS interop getCurrentPosition function.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrentPosition_InvokesJsInterop()
    {
        // Arrange
        var positionJson = JsonSerializer.SerializeToElement(new
        {
            latitude = 47.6062m,
            longitude = -122.3321m,
        });
        var moduleInterop = this.JSInterop.SetupModule("./js/geolocation.js");
        moduleInterop.Setup<JsonElement>("getCurrentPosition").SetResult(positionJson);

        var service = new GeolocationService(this.JSInterop.JSRuntime);

        // Act
        var result = await service.GetCurrentPositionAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(47.6062m, result.Latitude);
        Assert.Equal(-122.3321m, result.Longitude);
    }

    /// <summary>
    /// When JS interop throws (permission denied), returns failure result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrentPosition_WhenDenied_ReturnsFailure()
    {
        // Arrange
        var moduleInterop = this.JSInterop.SetupModule("./js/geolocation.js");
        moduleInterop.Setup<JsonElement>("getCurrentPosition")
            .SetException(new JSException("Location permission denied."));

        var service = new GeolocationService(this.JSInterop.JSRuntime);

        // Act
        var result = await service.GetCurrentPositionAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("permission denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// IsSupported returns true when the JS module reports support.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsSupported_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        var moduleInterop = this.JSInterop.SetupModule("./js/geolocation.js");
        moduleInterop.Setup<bool>("isSupported").SetResult(true);

        var service = new GeolocationService(this.JSInterop.JSRuntime);

        // Act
        var supported = await service.IsSupportedAsync();

        // Assert
        Assert.True(supported);
    }
}

// <copyright file="LocationFieldsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Transactions;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Transactions;

/// <summary>
/// Unit tests for the <see cref="LocationFields"/> component.
/// </summary>
public class LocationFieldsTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationFieldsTests"/> class.
    /// </summary>
    public LocationFieldsTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// When IsEnabled is true, shows input fields.
    /// </summary>
    [Fact]
    public void Render_WhenLocationEnabled_ShowsInputFields()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true));

        // Assert
        Assert.NotNull(cut.Find("#locCity"));
        Assert.NotNull(cut.Find("#locState"));
        Assert.NotNull(cut.Find("#locCountry"));
        Assert.NotNull(cut.Find("#locPostal"));
    }

    /// <summary>
    /// When IsEnabled is false, renders nothing.
    /// </summary>
    [Fact]
    public void Render_WhenLocationDisabled_RendersNothing()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, false));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Submit fires OnSave with correct DTO values.
    /// </summary>
    [Fact]
    public void Submit_CallsOnSaveWithCorrectDto()
    {
        // Arrange
        TransactionLocationUpdateDto? capturedDto = null;
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true)
            .Add(p => p.OnSave, (TransactionLocationUpdateDto dto) => capturedDto = dto));

        // Act — fill in fields and submit
        cut.Find("#locCity").Change("Seattle");
        cut.Find("#locState").Change("WA");
        cut.Find("#locCountry").Change("US");
        cut.Find("#locPostal").Change("98101");
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("Seattle", capturedDto.City);
        Assert.Equal("WA", capturedDto.StateOrRegion);
        Assert.Equal("US", capturedDto.Country);
        Assert.Equal("98101", capturedDto.PostalCode);
    }

    /// <summary>
    /// Clear button fires OnClear callback.
    /// </summary>
    [Fact]
    public void Clear_CallsOnClearCallback()
    {
        // Arrange
        var clearCalled = false;
        var location = new TransactionLocationDto
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Country = "US",
        };

        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true)
            .Add(p => p.Location, location)
            .Add(p => p.OnClear, () => clearCalled = true));

        // Act — click clear button
        cut.Find(".btn-clear-location").Click();

        // Assert
        Assert.True(clearCalled);
    }

    /// <summary>
    /// Verifies that existing location data pre-populates the form fields.
    /// </summary>
    [Fact]
    public void Render_WithExistingLocation_PrePopulatesFields()
    {
        // Arrange
        var location = new TransactionLocationDto
        {
            City = "Portland",
            StateOrRegion = "OR",
            Country = "US",
            PostalCode = "97201",
        };

        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true)
            .Add(p => p.Location, location));

        // Assert
        Assert.Equal("Portland", cut.Find("#locCity").GetAttribute("value"));
        Assert.Equal("OR", cut.Find("#locState").GetAttribute("value"));
        Assert.Equal("US", cut.Find("#locCountry").GetAttribute("value"));
        Assert.Equal("97201", cut.Find("#locPostal").GetAttribute("value"));
    }

    /// <summary>
    /// Verifies the Use Current Location button is rendered.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_ShowsUseLocationButton()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true));

        // Assert
        Assert.Contains("Use Current Location", cut.Markup);
    }

    /// <summary>
    /// Verifies GPS error message when geolocation service is null.
    /// </summary>
    [Fact]
    public void UseCurrentLocation_WithoutGeoService_ShowsError()
    {
        // Arrange
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true)
            .Add(p => p.GeolocationService, (GeolocationService?)null)
            .Add(p => p.BudgetApiService, (IBudgetApiService?)null));

        // Act
        cut.Find(".btn-use-location").Click();

        // Assert
        Assert.Contains("GPS services are not available", cut.Markup);
    }

    /// <summary>
    /// Verifies the clear button is only shown when location exists.
    /// </summary>
    [Fact]
    public void Render_WithoutLocation_HidesClearButton()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true));

        // Assert
        Assert.Empty(cut.FindAll(".btn-clear-location"));
    }

    /// <summary>
    /// Verifies Save Location button is present.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_ShowsSaveButton()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true));

        // Assert
        Assert.Contains("Save Location", cut.Markup);
    }

    /// <summary>
    /// Verifies the location display is rendered when location data exists.
    /// </summary>
    [Fact]
    public void Render_WithLocation_ShowsLocationDisplay()
    {
        // Arrange
        var location = new TransactionLocationDto
        {
            City = "Denver",
            StateOrRegion = "CO",
            Country = "US",
        };

        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true)
            .Add(p => p.Location, location));

        // Assert
        Assert.Contains("location-current", cut.Markup);
    }

    /// <summary>
    /// When enabled, the "Use Current Location" button is rendered.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_ShowsUseCurrentLocationButton()
    {
        // Act
        var cut = Render<LocationFields>(parameters => parameters
            .Add(p => p.IsEnabled, true));

        // Assert
        var btn = cut.Find(".btn-use-location");
        Assert.Contains("Use Current Location", btn.TextContent);
    }
}

// <copyright file="LocationDisplayTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Transactions;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Transactions;

/// <summary>
/// Unit tests for the <see cref="LocationDisplay"/> component.
/// </summary>
public class LocationDisplayTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationDisplayTests"/> class.
    /// </summary>
    public LocationDisplayTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// When location has city, state, and country, displays formatted text.
    /// </summary>
    [Fact]
    public void Render_WithFullLocation_ShowsFormatted()
    {
        // Arrange
        var location = new TransactionLocationDto
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Country = "US",
        };

        // Act
        var cut = Render<LocationDisplay>(parameters => parameters
            .Add(p => p.Location, location));

        // Assert
        var display = cut.Find(".location-display");
        Assert.Contains("Seattle", display.TextContent);
        Assert.Contains("WA", display.TextContent);
        Assert.Contains("US", display.TextContent);
    }

    /// <summary>
    /// When location has only city, displays just the city.
    /// </summary>
    [Fact]
    public void Render_WithCityOnly_ShowsCityOnly()
    {
        // Arrange
        var location = new TransactionLocationDto
        {
            City = "Portland",
        };

        // Act
        var cut = Render<LocationDisplay>(parameters => parameters
            .Add(p => p.Location, location));

        // Assert
        var display = cut.Find(".location-display");
        Assert.Contains("Portland", display.TextContent);
        Assert.DoesNotContain(",", display.TextContent.Trim());
    }

    /// <summary>
    /// When location is null, renders nothing.
    /// </summary>
    [Fact]
    public void Render_WithNullLocation_RendersNothing()
    {
        // Act
        var cut = Render<LocationDisplay>(parameters => parameters
            .Add(p => p.Location, (TransactionLocationDto?)null));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }
}

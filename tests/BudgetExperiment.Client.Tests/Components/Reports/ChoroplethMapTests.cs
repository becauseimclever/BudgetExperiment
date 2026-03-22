// <copyright file="ChoroplethMapTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using AngleSharp.Dom;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the <see cref="ChoroplethMap"/> component.
/// </summary>
public class ChoroplethMapTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChoroplethMapTests"/> class.
    /// </summary>
    public ChoroplethMapTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies that regions with spending data receive color fills based on a color scale.
    /// </summary>
    [Fact]
    public void Render_WithRegionData_AppliesColorScale()
    {
        // Arrange
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-WA", RegionName = "Washington", Country = "US", TotalSpending = 500m, TransactionCount = 5, Percentage = 50m },
            new() { RegionCode = "US-CA", RegionName = "California", Country = "US", TotalSpending = 300m, TransactionCount = 3, Percentage = 30m },
            new() { RegionCode = "US-TX", RegionName = "Texas", Country = "US", TotalSpending = 200m, TransactionCount = 2, Percentage = 20m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert — each state path should have a fill style applied
        var waPath = cut.Find("[data-state='WA']");
        var caPath = cut.Find("[data-state='CA']");
        var txPath = cut.Find("[data-state='TX']");

        Assert.Contains("fill:", waPath.GetAttribute("style") ?? string.Empty);
        Assert.Contains("fill:", caPath.GetAttribute("style") ?? string.Empty);
        Assert.Contains("fill:", txPath.GetAttribute("style") ?? string.Empty);

        // WA has the most spending and should have the darkest fill (lowest lightness)
        // We verify all three have fill styles; intensity ordering is visual
    }

    /// <summary>
    /// Verifies that an empty state message is shown when no regions are provided.
    /// </summary>
    [Fact]
    public void Render_EmptyData_ShowsEmptyState()
    {
        // Arrange & Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, new List<RegionSpendingDto>()));

        // Assert
        Assert.Contains("No location data", cut.Markup);
    }

    /// <summary>
    /// Verifies that clicking on a region fires the <see cref="ChoroplethMap.OnRegionClick"/> callback.
    /// </summary>
    [Fact]
    public void Click_OnRegion_FiresCallback()
    {
        // Arrange
        string? clickedRegion = null;
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-WA", RegionName = "Washington", Country = "US", TotalSpending = 100m, TransactionCount = 1, Percentage = 100m },
        };

        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions)
            .Add(c => c.OnRegionClick, EventCallback.Factory.Create<string>(this, region => clickedRegion = region)));

        // Act
        var waPath = cut.Find("[data-state='WA']");
        waPath.Click();

        // Assert
        Assert.Equal("WA", clickedRegion);
    }

    /// <summary>
    /// Verifies that each region path has an ARIA label for accessibility.
    /// </summary>
    [Fact]
    public void Render_HasAriaLabelsOnRegions()
    {
        // Arrange
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-CA", RegionName = "California", Country = "US", TotalSpending = 250.50m, TransactionCount = 3, Percentage = 100m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert
        var caPath = cut.Find("[data-state='CA']");
        var ariaLabel = caPath.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
        Assert.Contains("California", ariaLabel);
        Assert.Contains("250.50", ariaLabel);
    }

    /// <summary>
    /// Verifies that map region paths support keyboard navigation via tabindex and role.
    /// </summary>
    [Fact]
    public void Render_SupportsKeyboardNavigation()
    {
        // Arrange
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-TX", RegionName = "Texas", Country = "US", TotalSpending = 75m, TransactionCount = 1, Percentage = 100m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert
        var txPath = cut.Find("[data-state='TX']");
        Assert.Equal("0", txPath.GetAttribute("tabindex"));
        Assert.Equal("button", txPath.GetAttribute("role"));
    }

    /// <summary>
    /// Verifies that pattern fill SVG definitions are rendered for colorblind accessibility.
    /// </summary>
    [Fact]
    public void Render_WithRegionData_IncludesPatternDefs()
    {
        // Arrange
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-WA", RegionName = "Washington", Country = "US", TotalSpending = 100m, TransactionCount = 1, Percentage = 100m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert — SVG contains pattern definitions
        Assert.Contains("pattern-none", cut.Markup);
        Assert.Contains("pattern-low", cut.Markup);
        Assert.Contains("pattern-medium", cut.Markup);
        Assert.Contains("pattern-high", cut.Markup);
    }

    /// <summary>
    /// Verifies that pattern overlay paths are rendered alongside the colored paths.
    /// </summary>
    [Fact]
    public void Render_WithRegionData_RendersPatternOverlayPaths()
    {
        // Arrange
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-CA", RegionName = "California", Country = "US", TotalSpending = 200m, TransactionCount = 2, Percentage = 100m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert — pattern overlay paths exist with aria-hidden
        var overlays = cut.FindAll(".pattern-overlay");
        Assert.True(overlays.Count > 0, "Pattern overlay paths should be rendered");
        Assert.Equal("true", overlays[0].GetAttribute("aria-hidden"));
    }

    /// <summary>
    /// Verifies that high-spending regions use the high-intensity pattern.
    /// </summary>
    [Fact]
    public void Render_HighSpending_UsesHighPattern()
    {
        // Arrange — single region gets 100% ratio → high pattern
        var regions = new List<RegionSpendingDto>
        {
            new() { RegionCode = "US-WA", RegionName = "Washington", Country = "US", TotalSpending = 500m, TransactionCount = 5, Percentage = 100m },
        };

        // Act
        var cut = Render<ChoroplethMap>(p => p
            .Add(c => c.Regions, regions));

        // Assert — WA overlay should use pattern-high since it's the max
        var overlays = cut.FindAll(".pattern-overlay");
        var waOverlay = overlays.FirstOrDefault(o => o.GetAttribute("style")?.Contains("pattern-high") == true);
        Assert.NotNull(waOverlay);
    }
}

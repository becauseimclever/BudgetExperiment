// <copyright file="BudgetTreemapTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.BudgetTreemap (Razor component)
//     located in src/.../Charts/ApexCharts/BudgetTreemap.razor
// They define the expected parameter API and rendered HTML contract.
// BudgetTreemap is backed by Blazor-ApexCharts (IJSRuntime required).
// JSInterop is set to Loose so ApexCharts JS calls do not throw in bUnit.
// DO NOT assert on SVG or canvas content — ApexCharts renders that via JS
// and it will not appear in the bUnit render tree.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="BudgetTreemap"/> component.
/// </summary>
public class BudgetTreemapTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetTreemapTests"/> class.
    /// Sets JSInterop to Loose so Blazor-ApexCharts JS calls do not throw in bUnit.
    /// </summary>
    public BudgetTreemapTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies the empty-state element is rendered when an empty data list is supplied.
    /// </summary>
    [Fact]
    public void BudgetTreemap_Renders_EmptyState_WhenNoDataPoints()
    {
        // Act
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, new List<TreemapDataPoint>()));

        // Assert
        var empty = cut.Find(".budget-treemap-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    /// <summary>
    /// Verifies the outer container carries the aria-label supplied via parameter.
    /// </summary>
    [Fact]
    public void BudgetTreemap_Renders_OuterContainer_WithAriaLabel()
    {
        // Arrange
        const string ariaLabel = "Spending by category";
        var dataPoints = new List<TreemapDataPoint>
        {
            new("Groceries", 450m),
        };

        // Act
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.AriaLabel, ariaLabel));

        // Assert
        var container = cut.Find(".budget-treemap");
        Assert.Equal(ariaLabel, container.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies the outer container has the expected CSS class when data is provided.
    /// </summary>
    [Fact]
    public void BudgetTreemap_Renders_OuterContainer_WithCorrectClass()
    {
        // Arrange
        var dataPoints = new List<TreemapDataPoint>
        {
            new("Housing", 1200m),
        };

        // Act
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.AriaLabel, "Test treemap"));

        // Assert
        var container = cut.Find(".budget-treemap");
        Assert.NotNull(container);
    }

    /// <summary>
    /// Verifies the empty-state element is absent when data points are provided.
    /// </summary>
    [Fact]
    public void BudgetTreemap_DoesNotRender_EmptyState_WhenDataProvided()
    {
        // Arrange
        var dataPoints = new List<TreemapDataPoint>
        {
            new("Dining", 320m),
        };

        // Act
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.AriaLabel, "Spending treemap"));

        // Assert
        Assert.Empty(cut.FindAll(".budget-treemap-empty"));
    }

    /// <summary>
    /// Verifies the outer container carries the default aria-label when none is supplied.
    /// </summary>
    [Fact]
    public void BudgetTreemap_Renders_WithDefaultAriaLabel_WhenNotSet()
    {
        // Arrange
        var dataPoints = new List<TreemapDataPoint>
        {
            new("Transport", 90m),
        };

        // Act
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints));

        // Assert — default is "Category spending treemap"
        var container = cut.Find(".budget-treemap");
        Assert.Equal("Category spending treemap", container.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies the component renders without throwing an exception when given multiple data points.
    /// </summary>
    [Fact]
    public void BudgetTreemap_DoesNotThrow_WhenRenderingWithMultipleDataPoints()
    {
        // Arrange — 5 data points
        var dataPoints = new List<TreemapDataPoint>
        {
            new("Groceries", 450m, "#4e79a7"),
            new("Housing", 1200m, "#f28e2b"),
            new("Dining", 320m, "#e15759"),
            new("Transport", 90m, "#76b7b2"),
            new("Utilities", 150m, "#59a14f"),
        };

        // Act + Assert — no exception
        var cut = Render<BudgetTreemap>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.AriaLabel, "Full treemap"));

        Assert.NotNull(cut);
    }
}

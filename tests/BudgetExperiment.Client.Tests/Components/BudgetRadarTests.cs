// <copyright file="BudgetRadarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.BudgetRadar (Razor component)
//     located in src/.../Charts/ApexCharts/BudgetRadar.razor
// They define the expected parameter API and rendered HTML contract.
// BudgetRadar is backed by Blazor-ApexCharts (IJSRuntime required).
// JSInterop is set to Loose so ApexCharts JS calls do not throw in bUnit.
// DO NOT assert on SVG or canvas content — ApexCharts renders that via JS
// and it will not appear in the bUnit render tree.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="BudgetRadar"/> component.
/// </summary>
public class BudgetRadarTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetRadarTests"/> class.
    /// Sets JSInterop to Loose so Blazor-ApexCharts JS calls do not throw in bUnit.
    /// </summary>
    public BudgetRadarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies the empty-state element is rendered when an empty series list is supplied.
    /// </summary>
    [Fact]
    public void BudgetRadar_Renders_EmptyState_WhenNoSeries()
    {
        // Arrange
        var categories = new List<string> { "Jan", "Feb", "Mar" };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, new List<RadarDataSeries>())
            .Add(p => p.Categories, categories));

        // Assert
        var empty = cut.Find(".budget-radar-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    /// <summary>
    /// Verifies the empty-state element is rendered when series are provided but the categories list is empty.
    /// </summary>
    [Fact]
    public void BudgetRadar_Renders_EmptyState_WhenNoCategories()
    {
        // Arrange
        var series = new List<RadarDataSeries>
        {
            new("Actual", new List<decimal> { 450m, 320m, 90m }),
        };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Categories, new List<string>()));

        // Assert
        var empty = cut.Find(".budget-radar-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    /// <summary>
    /// Verifies the outer container carries the aria-label supplied via parameter.
    /// </summary>
    [Fact]
    public void BudgetRadar_Renders_OuterContainer_WithAriaLabel()
    {
        // Arrange
        const string ariaLabel = "Monthly category radar";
        var series = new List<RadarDataSeries>
        {
            new("Actual", new List<decimal> { 450m, 320m, 90m }),
        };
        var categories = new List<string> { "Groceries", "Dining", "Transport" };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Categories, categories)
            .Add(p => p.AriaLabel, ariaLabel));

        // Assert
        var container = cut.Find(".budget-radar");
        Assert.Equal(ariaLabel, container.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies the outer container has the expected CSS class when data is provided.
    /// </summary>
    [Fact]
    public void BudgetRadar_Renders_OuterContainer_WithCorrectClass()
    {
        // Arrange
        var series = new List<RadarDataSeries>
        {
            new("Budget", new List<decimal> { 500m, 400m }),
        };
        var categories = new List<string> { "Food", "Housing" };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Categories, categories)
            .Add(p => p.AriaLabel, "Test radar"));

        // Assert
        var container = cut.Find(".budget-radar");
        Assert.NotNull(container);
    }

    /// <summary>
    /// Verifies the empty-state element is absent when both series and categories are provided.
    /// </summary>
    [Fact]
    public void BudgetRadar_DoesNotRender_EmptyState_WhenDataProvided()
    {
        // Arrange
        var series = new List<RadarDataSeries>
        {
            new("Actual", new List<decimal> { 450m, 320m, 90m }),
        };
        var categories = new List<string> { "Groceries", "Dining", "Transport" };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Categories, categories)
            .Add(p => p.AriaLabel, "Spend radar"));

        // Assert
        Assert.Empty(cut.FindAll(".budget-radar-empty"));
    }

    /// <summary>
    /// Verifies the outer container carries the default aria-label when none is supplied.
    /// </summary>
    [Fact]
    public void BudgetRadar_Renders_WithDefaultAriaLabel_WhenNotSet()
    {
        // Arrange
        var series = new List<RadarDataSeries>
        {
            new("Actual", new List<decimal> { 200m, 150m }),
        };
        var categories = new List<string> { "Food", "Transport" };

        // Act
        var cut = Render<BudgetRadar>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Categories, categories));

        // Assert — default is "Budget spend radar chart"
        var container = cut.Find(".budget-radar");
        Assert.Equal("Budget spend radar chart", container.GetAttribute("aria-label"));
    }
}

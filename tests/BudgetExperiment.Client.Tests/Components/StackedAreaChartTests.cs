// <copyright file="StackedAreaChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.StackedAreaChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the StackedAreaChart component.
/// </summary>
public class StackedAreaChartTests : BunitContext
{
    private static readonly DateOnly _start = new DateOnly(2026, 1, 1);

    [Fact]
    public void StackedAreaChart_Renders_EmptyState_WhenNoSeries()
    {
        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, new List<StackedAreaSeries>()));

        // Assert
        var empty = cut.Find(".stacked-area-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void StackedAreaChart_Renders_EmptyState_WhenSeriesHaveNoPoints()
    {
        // Arrange — series exist but contain zero data points
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", new List<StackedAreaDataPoint>()),
            new("Food", "#f28e2b", new List<StackedAreaDataPoint>()),
        };

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert
        var empty = cut.Find(".stacked-area-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void StackedAreaChart_Renders_SVG_WhenDataProvided()
    {
        // Arrange — one series with three data points
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", new List<StackedAreaDataPoint>
            {
                new(_start, 1200m),
                new(_start.AddMonths(1), 1250m),
                new(_start.AddMonths(2), 1300m),
            }),
        };

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert
        var svg = cut.Find("svg.stacked-area-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void StackedAreaChart_Renders_OnePathPerSeries()
    {
        // Arrange — 3 series, each with 3 data points
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", BuildPoints(1200m, 1250m, 1300m)),
            new("Food", "#f28e2b", BuildPoints(400m, 420m, 380m)),
            new("Transport", "#76b7b2", BuildPoints(150m, 160m, 140m)),
        };

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert
        var paths = cut.FindAll("path.stacked-area-path");
        Assert.Equal(3, paths.Count);
    }

    [Fact]
    public void StackedAreaChart_Path_HasFillAttribute()
    {
        // Arrange
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", BuildPoints(1200m, 1250m, 1300m)),
        };

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert — path must have a fill that is not "none" (areas must be filled)
        var path = cut.Find("path.stacked-area-path");
        var fill = path.GetAttribute("fill");
        Assert.NotNull(fill);
        Assert.NotEqual("none", fill, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void StackedAreaChart_Renders_Axis()
    {
        // Arrange
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", BuildPoints(1200m, 1250m, 1300m)),
        };

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert — at least one axis element must be present
        var axes = cut.FindAll(".stacked-area-axis");
        Assert.NotEmpty(axes);
    }

    [Fact]
    public void StackedAreaChart_UsesAriaLabel()
    {
        // Arrange
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", BuildPoints(1200m, 1250m, 1300m)),
        };

        const string label = "Monthly budget area chart";

        // Act
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.AriaLabel, label));

        // Assert
        var container = cut.Find(".stacked-area-chart");
        Assert.Equal(label, container.GetAttribute("aria-label"));
    }

    [Fact]
    public void StackedAreaChart_SinglePointSeries_RendersWithoutError()
    {
        // Arrange — boundary condition: exactly one data point per series
        var series = new List<StackedAreaSeries>
        {
            new("Housing", "#4e79a7", new List<StackedAreaDataPoint>
            {
                new(_start, 1200m),
            }),
        };

        // Act — must not throw
        var cut = Render<StackedAreaChart>(parameters => parameters
            .Add(p => p.Series, series));

        // Assert — component rendered (no exception means success; SVG must be present)
        var svg = cut.Find("svg.stacked-area-svg");
        Assert.NotNull(svg);
    }

    private static IReadOnlyList<StackedAreaDataPoint> BuildPoints(params decimal[] amounts)
    {
        var points = new List<StackedAreaDataPoint>();
        for (var i = 0; i < amounts.Length; i++)
        {
            points.Add(new StackedAreaDataPoint(_start.AddMonths(i), amounts[i]));
        }

        return points;
    }
}

// <copyright file="LineChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the LineChart component.
/// </summary>
public class LineChartTests : BunitContext
{
    [Fact]
    public void LineChart_Renders_EmptyState_WhenNoData()
    {
        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, new List<LineData>()));

        // Assert
        var empty = cut.Find(".line-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void LineChart_Renders_Path_WhenDataProvided()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
            new() { Label = "Feb", Value = 400m },
            new() { Label = "Mar", Value = 300m },
        };

        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var path = cut.FindAll("path.line-path");
        Assert.Single(path);
    }

    [Fact]
    public void LineChart_Renders_Points_WhenEnabled()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
            new() { Label = "Feb", Value = 400m },
        };

        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowPoints, true));

        // Assert
        var points = cut.FindAll("circle.line-point");
        Assert.Equal(2, points.Count);
    }

    [Fact]
    public void LineChart_Hides_Points_WhenDisabled()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
            new() { Label = "Feb", Value = 400m },
        };

        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowPoints, false));

        // Assert
        var points = cut.FindAll("circle.line-point");
        Assert.Empty(points);
    }

    [Fact]
    public void LineChart_Renders_MultiSeries_WhenDefined()
    {
        // Arrange
        var data = new List<LineData>
        {
            new()
            {
                Label = "Jan",
                Value = 0m,
                SeriesValues = new Dictionary<string, decimal>
                {
                    ["income"] = 1000m,
                    ["spending"] = 400m,
                },
            },
            new()
            {
                Label = "Feb",
                Value = 0m,
                SeriesValues = new Dictionary<string, decimal>
                {
                    ["income"] = 1200m,
                    ["spending"] = 600m,
                },
            },
        };

        var series = new List<LineSeriesDefinition>
        {
            new() { Id = "income", Label = "Income", Color = "#10B981" },
            new() { Id = "spending", Label = "Spending", Color = "#EF4444" },
        };

        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series));

        // Assert
        var paths = cut.FindAll("path.line-path");
        Assert.Equal(2, paths.Count);
    }

    [Fact]
    public void LineChart_Uses_AriaLabel()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
        };

        // Act
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.AriaLabel, "Monthly trend"));

        // Assert
        var svg = cut.Find("svg.line-chart");
        Assert.Equal("Monthly trend", svg.GetAttribute("aria-label"));
    }
}

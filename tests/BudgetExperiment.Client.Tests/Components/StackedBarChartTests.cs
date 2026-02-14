// <copyright file="StackedBarChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the StackedBarChart component.
/// </summary>
public class StackedBarChartTests : BunitContext
{
    [Fact]
    public void StackedBarChart_Renders_EmptyState_WhenNoData()
    {
        // Act
        var cut = Render<StackedBarChart>(parameters => parameters
            .Add(p => p.Data, new List<GroupedBarData>()));

        // Assert
        var empty = cut.Find(".stacked-bar-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void StackedBarChart_Renders_Segments_WhenDataProvided()
    {
        // Arrange
        var data = new List<GroupedBarData>
        {
            new()
            {
                GroupId = "jan",
                GroupLabel = "Jan",
                Values = new Dictionary<string, decimal>
                {
                    ["income"] = 1200m,
                    ["spending"] = 500m,
                },
            },
            new()
            {
                GroupId = "feb",
                GroupLabel = "Feb",
                Values = new Dictionary<string, decimal>
                {
                    ["income"] = 1100m,
                    ["spending"] = 600m,
                },
            },
        };

        var series = new List<BarSeriesDefinition>
        {
            new() { Id = "income", Label = "Income", Color = "#10B981" },
            new() { Id = "spending", Label = "Spending", Color = "#EF4444" },
        };

        // Act
        var cut = Render<StackedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series));

        // Assert
        var segments = cut.FindAll("rect.stacked-bar-segment");
        Assert.Equal(4, segments.Count);
    }

    [Fact]
    public void StackedBarChart_Renders_Legend_WhenEnabled()
    {
        // Arrange
        var data = new List<GroupedBarData>
        {
            new()
            {
                GroupId = "jan",
                GroupLabel = "Jan",
                Values = new Dictionary<string, decimal>
                {
                    ["income"] = 1200m,
                },
            },
        };

        var series = new List<BarSeriesDefinition>
        {
            new() { Id = "income", Label = "Income", Color = "#10B981" },
            new() { Id = "spending", Label = "Spending", Color = "#EF4444" },
        };

        // Act
        var cut = Render<StackedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, true));

        // Assert
        var legend = cut.Find(".stacked-bar-legend");
        Assert.NotNull(legend);
        Assert.Equal(2, cut.FindAll(".legend-item").Count);
    }

    [Fact]
    public void StackedBarChart_Uses_AriaLabel()
    {
        // Arrange
        var data = new List<GroupedBarData>
        {
            new()
            {
                GroupId = "jan",
                GroupLabel = "Jan",
                Values = new Dictionary<string, decimal>
                {
                    ["income"] = 1200m,
                },
            },
        };

        // Act
        var cut = Render<StackedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.AriaLabel, "Stacked chart"));

        // Assert
        var svg = cut.Find("svg.stacked-bar-chart");
        Assert.Equal("Stacked chart", svg.GetAttribute("aria-label"));
    }

    [Fact]
    public void StackedBarChart_Segment_Heights_Respect_Values()
    {
        // Arrange
        var data = new List<GroupedBarData>
        {
            new()
            {
                GroupId = "jan",
                GroupLabel = "Jan",
                Values = new Dictionary<string, decimal>
                {
                    ["income"] = 1200m,
                    ["spending"] = 400m,
                },
            },
        };

        var series = new List<BarSeriesDefinition>
        {
            new() { Id = "income", Label = "Income", Color = "#10B981" },
            new() { Id = "spending", Label = "Spending", Color = "#EF4444" },
        };

        // Act
        var cut = Render<StackedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series));

        // Assert
        var segments = cut.FindAll("rect.stacked-bar-segment");
        Assert.Equal(2, segments.Count);

        var firstHeight = double.Parse(segments[0].GetAttribute("height") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        var secondHeight = double.Parse(segments[1].GetAttribute("height") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(firstHeight > secondHeight);
    }
}

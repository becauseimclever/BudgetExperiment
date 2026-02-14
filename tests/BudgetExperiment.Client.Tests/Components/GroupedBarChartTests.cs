// <copyright file="GroupedBarChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the GroupedBarChart component.
/// </summary>
public class GroupedBarChartTests : BunitContext
{
    [Fact]
    public void GroupedBarChart_Renders_EmptyState_WhenNoData()
    {
        // Act
        var cut = Render<GroupedBarChart>(parameters => parameters
            .Add(p => p.Data, new List<GroupedBarData>()));

        // Assert
        var empty = cut.Find(".grouped-bar-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void GroupedBarChart_Renders_Bars_WhenDataProvided()
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
        var cut = Render<GroupedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series));

        // Assert
        var bars = cut.FindAll("rect.grouped-bar-rect");
        Assert.Equal(4, bars.Count);
    }

    [Fact]
    public void GroupedBarChart_Renders_Legend_WhenEnabled()
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
        var cut = Render<GroupedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, true));

        // Assert
        var legend = cut.Find(".grouped-bar-legend");
        Assert.NotNull(legend);
        Assert.Equal(2, cut.FindAll(".legend-item").Count);
    }

    [Fact]
    public void GroupedBarChart_Uses_AriaLabel()
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
        var cut = Render<GroupedBarChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.AriaLabel, "Grouped chart"));

        // Assert
        var svg = cut.Find("svg.grouped-bar-chart");
        Assert.Equal("Grouped chart", svg.GetAttribute("aria-label"));
    }
}

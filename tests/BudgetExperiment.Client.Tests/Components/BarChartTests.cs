// <copyright file="BarChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the BarChart component.
/// </summary>
public class BarChartTests : BunitContext
{
    [Fact]
    public void BarChart_Renders_EmptyState_WhenNoGroups()
    {
        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, new List<BarChartGroup>()));

        // Assert
        var empty = cut.Find(".bar-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void BarChart_Renders_Bars_WhenDataProvided()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values =
                [
                    new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" },
                    new BarChartValue { Series = "Income", Value = 1000m, Color = "#10B981" },
                ],
            },
            new()
            {
                Label = "Feb",
                Values =
                [
                    new BarChartValue { Series = "Spending", Value = 600m, Color = "#EF4444" },
                    new BarChartValue { Series = "Income", Value = 1200m, Color = "#10B981" },
                ],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups));

        // Assert
        var svg = cut.Find("svg.bar-chart");
        Assert.NotNull(svg);

        var bars = cut.FindAll("rect.bar-rect");
        Assert.Equal(4, bars.Count); // 2 groups × 2 bars each
    }

    [Fact]
    public void BarChart_Renders_SingleBar_PerGroup()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
            new()
            {
                Label = "Feb",
                Values = [new BarChartValue { Series = "Spending", Value = 700m, Color = "#EF4444" }],
            },
            new()
            {
                Label = "Mar",
                Values = [new BarChartValue { Series = "Spending", Value = 300m, Color = "#EF4444" }],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups));

        // Assert
        var bars = cut.FindAll("rect.bar-rect");
        Assert.Equal(3, bars.Count);
    }

    [Fact]
    public void BarChart_Renders_Legend_WhenEnabled()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
        };

        var series = new List<BarChartSeries>
        {
            new() { Name = "Spending", Color = "#EF4444" },
            new() { Name = "Income", Color = "#10B981" },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups)
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, true));

        // Assert
        var legend = cut.Find(".bar-chart-legend");
        Assert.NotNull(legend);

        var legendItems = cut.FindAll(".legend-item");
        Assert.Equal(2, legendItems.Count);
    }

    [Fact]
    public void BarChart_HidesLegend_WhenDisabled()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
        };

        var series = new List<BarChartSeries>
        {
            new() { Name = "Spending", Color = "#EF4444" },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups)
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, false));

        // Assert
        var legends = cut.FindAll(".bar-chart-legend");
        Assert.Empty(legends);
    }

    [Fact]
    public void BarChart_HasAccessibility_AriaLabel()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups)
            .Add(p => p.AriaLabel, "Monthly spending trends"));

        // Assert
        var svg = cut.Find("svg.bar-chart");
        Assert.Equal("Monthly spending trends", svg.GetAttribute("aria-label"));
    }

    [Fact]
    public void BarChart_Bars_HaveAriaLabels()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups));

        // Assert
        var bar = cut.Find("rect.bar-rect");
        var ariaLabel = bar.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
        Assert.Contains("Spending", ariaLabel);
        Assert.Contains("Jan", ariaLabel);
    }

    [Fact]
    public void BarChart_Renders_XAxisLabels()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "January",
                Values = [new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" }],
            },
            new()
            {
                Label = "February",
                Values = [new BarChartValue { Series = "Spending", Value = 700m, Color = "#EF4444" }],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups));

        // Assert
        var xLabels = cut.FindAll("text.x-axis-label");
        Assert.Equal(2, xLabels.Count);
        Assert.Contains("January", xLabels[0].TextContent);
        Assert.Contains("February", xLabels[1].TextContent);
    }

    [Fact]
    public void BarChart_Bars_HaveCorrectColors()
    {
        // Arrange
        var groups = new List<BarChartGroup>
        {
            new()
            {
                Label = "Jan",
                Values =
                [
                    new BarChartValue { Series = "Spending", Value = 500m, Color = "#EF4444" },
                    new BarChartValue { Series = "Income", Value = 1000m, Color = "#10B981" },
                ],
            },
        };

        // Act
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Groups, groups));

        // Assert
        var bars = cut.FindAll("rect.bar-rect");
        Assert.Equal("#EF4444", bars[0].GetAttribute("fill"));
        Assert.Equal("#10B981", bars[1].GetAttribute("fill"));
    }
}

// <copyright file="DonutChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the DonutChart component.
/// </summary>
public class DonutChartTests : BunitContext
{
    [Fact]
    public void DonutChart_Renders_EmptyState_WhenNoSegments()
    {
        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, new List<DonutSegmentData>())
            .Add(p => p.CenterValue, 0m)
            .Add(p => p.CenterLabel, "Total"));

        // Assert
        var svg = cut.Find("svg.donut-chart");
        Assert.NotNull(svg);

        // Should have empty state circle
        var circles = cut.FindAll("circle");
        Assert.Single(circles);
    }

    [Fact]
    public void DonutChart_Renders_Segments_WhenDataProvided()
    {
        // Arrange
        var segments = new List<DonutSegmentData>
        {
            new()
            {
                Id = "1",
                Label = "Groceries",
                Value = 500m,
                Percentage = 50m,
                Color = "#10B981",
                TransactionCount = 10,
            },
            new()
            {
                Id = "2",
                Label = "Entertainment",
                Value = 500m,
                Percentage = 50m,
                Color = "#8B5CF6",
                TransactionCount = 5,
            },
        };

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 1000m)
            .Add(p => p.CenterLabel, "Total"));

        // Assert
        var svg = cut.Find("svg.donut-chart");
        Assert.NotNull(svg);

        // Should have segment circles (one per segment)
        var circles = cut.FindAll("circle.donut-segment");
        Assert.Equal(2, circles.Count);
    }

    [Fact]
    public void DonutChart_Displays_CenterValue()
    {
        // Arrange
        var segments = new List<DonutSegmentData>
        {
            new()
            {
                Id = "1",
                Label = "Test",
                Value = 500m,
                Percentage = 100m,
                Color = "#10B981",
                TransactionCount = 1,
            },
        };

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 1234m)
            .Add(p => p.CenterLabel, "Spent"));

        // Assert
        var centerValue = cut.Find("text.donut-center-value");
        Assert.Contains("1,234", centerValue.TextContent);

        var centerLabel = cut.Find("text.donut-center-label");
        Assert.Equal("Spent", centerLabel.TextContent);
    }

    [Fact]
    public void DonutChart_Renders_Legend_WhenEnabled()
    {
        // Arrange
        var segments = new List<DonutSegmentData>
        {
            new()
            {
                Id = "1",
                Label = "Groceries",
                Value = 300m,
                Percentage = 60m,
                Color = "#10B981",
                TransactionCount = 5,
            },
            new()
            {
                Id = "2",
                Label = "Gas",
                Value = 200m,
                Percentage = 40m,
                Color = "#F59E0B",
                TransactionCount = 3,
            },
        };

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 500m)
            .Add(p => p.ShowLegend, true));

        // Assert
        var legend = cut.Find(".chart-legend");
        Assert.NotNull(legend);

        var legendItems = cut.FindAll(".legend-item");
        Assert.Equal(2, legendItems.Count);
    }

    [Fact]
    public void DonutChart_HidesLegend_WhenDisabled()
    {
        // Arrange
        var segments = new List<DonutSegmentData>
        {
            new()
            {
                Id = "1",
                Label = "Test",
                Value = 100m,
                Percentage = 100m,
                Color = "#10B981",
                TransactionCount = 1,
            },
        };

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 100m)
            .Add(p => p.ShowLegend, false));

        // Assert
        var legends = cut.FindAll(".chart-legend");
        Assert.Empty(legends);
    }

    [Fact]
    public void DonutChart_Segment_HasCorrectColor()
    {
        // Arrange
        var segments = new List<DonutSegmentData>
        {
            new()
            {
                Id = "1",
                Label = "Test Category",
                Value = 100m,
                Percentage = 100m,
                Color = "#FF5733",
                TransactionCount = 1,
            },
        };

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 100m));

        // Assert
        var segment = cut.Find("circle.donut-segment");
        Assert.Equal("#FF5733", segment.GetAttribute("stroke"));
    }

    [Fact]
    public void DonutChart_AppliesCompactClass_WhenCompact()
    {
        // Arrange
        var segments = new List<DonutSegmentData>();

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.CenterValue, 0m)
            .Add(p => p.Compact, true));

        // Assert
        var container = cut.Find(".donut-chart-container");
        Assert.Contains("compact", container.ClassList);
    }

    [Fact]
    public void DonutChart_HasAccessibility_AriaLabel()
    {
        // Arrange
        var segments = new List<DonutSegmentData>();

        // Act
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.AriaLabel, "Monthly spending breakdown"));

        // Assert
        var svg = cut.Find("svg.donut-chart");
        Assert.Equal("Monthly spending breakdown", svg.GetAttribute("aria-label"));
    }
}

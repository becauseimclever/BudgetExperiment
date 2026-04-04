// <copyright file="WaterfallChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.WaterfallChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the WaterfallChart component.
/// </summary>
public class WaterfallChartTests : BunitContext
{
    [Fact]
    public void WaterfallChart_Renders_EmptyState_WhenNoSegments()
    {
        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, new List<WaterfallSegment>()));

        // Assert
        var empty = cut.Find(".waterfall-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void WaterfallChart_Renders_SVG_WhenSegmentsProvided()
    {
        // Arrange — two incremental segments
        var segments = new List<WaterfallSegment>
        {
            new("Income", 2000m, 2000m, IsTotal: false),
            new("Rent", -800m, 1200m, IsTotal: false),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var svg = cut.Find("svg.waterfall-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void WaterfallChart_Renders_OneBarPerSegment()
    {
        // Arrange — 4 segments
        var segments = new List<WaterfallSegment>
        {
            new("Income", 3000m, 3000m, IsTotal: false),
            new("Rent", -1000m, 2000m, IsTotal: false),
            new("Groceries", -400m, 1600m, IsTotal: false),
            new("Net", 1600m, 1600m, IsTotal: true),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var bars = cut.FindAll("rect.waterfall-bar");
        Assert.Equal(4, bars.Count);
    }

    [Fact]
    public void WaterfallChart_PositiveSegment_HasPositiveClass()
    {
        // Arrange — Amount >= 0 → IsPositive = true, IsTotal = false
        var segments = new List<WaterfallSegment>
        {
            new("Income", 2000m, 2000m, IsTotal: false),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var bar = cut.Find("rect.waterfall-bar");
        Assert.Contains("waterfall-positive", bar.ClassName ?? string.Empty);
    }

    [Fact]
    public void WaterfallChart_NegativeSegment_HasNegativeClass()
    {
        // Arrange — Amount < 0 → IsPositive = false, IsTotal = false
        var segments = new List<WaterfallSegment>
        {
            new("Rent", -800m, -800m, IsTotal: false),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var bar = cut.Find("rect.waterfall-bar");
        Assert.Contains("waterfall-negative", bar.ClassName ?? string.Empty);
    }

    [Fact]
    public void WaterfallChart_TotalSegment_HasTotalClass()
    {
        // Arrange — IsTotal = true
        var segments = new List<WaterfallSegment>
        {
            new("Total", 1200m, 1200m, IsTotal: true),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var bar = cut.Find("rect.waterfall-bar");
        Assert.Contains("waterfall-total", bar.ClassName ?? string.Empty);
    }

    [Fact]
    public void WaterfallChart_Renders_Connectors_BetweenBars()
    {
        // Arrange — 3 segments → connectors drawn between adjacent bars
        var segments = new List<WaterfallSegment>
        {
            new("Income", 3000m, 3000m, IsTotal: false),
            new("Rent", -1000m, 2000m, IsTotal: false),
            new("Net", 2000m, 2000m, IsTotal: true),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert — at least one connector line rendered between bars
        var connectors = cut.FindAll("line.waterfall-connector");
        Assert.NotEmpty(connectors);
    }

    [Fact]
    public void WaterfallChart_Renders_LabelPerSegment()
    {
        // Arrange — 2 segments
        var segments = new List<WaterfallSegment>
        {
            new("Income", 2000m, 2000m, IsTotal: false),
            new("Rent", -800m, 1200m, IsTotal: false),
        };

        // Act
        var cut = Render<WaterfallChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert — one label element per segment
        var labels = cut.FindAll(".waterfall-label");
        Assert.Equal(2, labels.Count);
    }
}

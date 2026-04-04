// <copyright file="RadialBarChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.RadialBarChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the RadialBarChart component.
/// </summary>
public class RadialBarChartTests : BunitContext
{
    [Fact]
    public void RadialBarChart_Renders_EmptyState_WhenNoSegments()
    {
        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, new List<RadialBarSegment>()));

        // Assert
        var empty = cut.Find(".radial-bar-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data", empty.TextContent);
    }

    [Fact]
    public void RadialBarChart_Renders_SVG_WhenSegmentsProvided()
    {
        // Arrange
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 850m, 1200m, "#4e79a7"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var svg = cut.Find("svg.radial-bar-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void RadialBarChart_Renders_TrackAndArc_PerSegment()
    {
        // Arrange — 2 segments → 2 track circles + 2 arc circles
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 850m, 1200m, "#4e79a7"),
            new("Food", 320m, 500m, "#f28e2b"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var tracks = cut.FindAll("circle.radial-bar-track");
        var arcs = cut.FindAll("circle.radial-bar-arc");
        Assert.Equal(2, tracks.Count);
        Assert.Equal(2, arcs.Count);
    }

    [Fact]
    public void RadialBarChart_Arc_HasStrokeDasharray()
    {
        // Arrange
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 850m, 1200m, "#4e79a7"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert — each arc circle must carry a stroke-dasharray for the progress animation
        var arc = cut.Find("circle.radial-bar-arc");
        var dasharray = arc.GetAttribute("stroke-dasharray");
        Assert.NotNull(dasharray);
        Assert.NotEmpty(dasharray);
    }

    [Fact]
    public void RadialBarChart_Renders_LabelPerSegment()
    {
        // Arrange — 3 segments → 3 label elements
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 850m, 1200m, "#4e79a7"),
            new("Food", 320m, 500m, "#f28e2b"),
            new("Transport", 90m, 200m, "#76b7b2"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert
        var labels = cut.FindAll(".radial-bar-label");
        Assert.Equal(3, labels.Count);
    }

    [Fact]
    public void RadialBarChart_UsesAriaLabel()
    {
        // Arrange
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 850m, 1200m, "#4e79a7"),
        };

        const string label = "April budget utilization";

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.AriaLabel, label));

        // Assert
        var container = cut.Find(".radial-bar-chart");
        Assert.Equal(label, container.GetAttribute("aria-label"));
    }

    [Fact]
    public void RadialBarChart_ZeroValueSegment_HasStrokeDashoffsetAttribute()
    {
        // Arrange — zero-value segment: arc should appear fully hidden (offset ≥ circumference)
        var segments = new List<RadialBarSegment>
        {
            new("Savings", 0m, 500m, "#59a14f"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert — attribute must exist; its value being ≥ dasharray implies invisible arc
        var arc = cut.Find("circle.radial-bar-arc");
        var dashoffset = arc.GetAttribute("stroke-dashoffset");
        Assert.NotNull(dashoffset);
    }

    [Fact]
    public void RadialBarChart_FullValueSegment_HasNonNegativeDashoffset()
    {
        // Arrange — fully utilized segment: arc is completely drawn (stroke-dashoffset near 0)
        var segments = new List<RadialBarSegment>
        {
            new("Housing", 1200m, 1200m, "#4e79a7"),
        };

        // Act
        var cut = Render<RadialBarChart>(parameters => parameters
            .Add(p => p.Segments, segments));

        // Assert — attribute must exist and parse to a numeric value ≥ 0
        var arc = cut.Find("circle.radial-bar-arc");
        var dashoffset = arc.GetAttribute("stroke-dashoffset");
        Assert.NotNull(dashoffset);
        Assert.True(double.TryParse(dashoffset, out var parsed));
        Assert.True(parsed >= 0d);
    }
}

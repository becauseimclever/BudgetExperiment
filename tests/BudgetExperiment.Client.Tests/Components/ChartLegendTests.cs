// <copyright file="ChartLegendTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="ChartLegend"/> component.
/// </summary>
public class ChartLegendTests : BunitContext
{
    /// <summary>
    /// Verifies the legend renders nothing when there are no segments.
    /// </summary>
    [Fact]
    public void ChartLegend_RendersEmpty_WhenNoSegments()
    {
        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, Array.Empty<DonutSegmentData>()));

        Assert.Empty(cut.FindAll(".legend-item"));
    }

    /// <summary>
    /// Verifies the legend renders one item per segment.
    /// </summary>
    [Fact]
    public void ChartLegend_RendersOneItem_PerSegment()
    {
        var segments = CreateSegments(3);

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments));

        Assert.Equal(3, cut.FindAll(".legend-item").Count);
    }

    /// <summary>
    /// Verifies the legend shows segment labels.
    /// </summary>
    [Fact]
    public void ChartLegend_ShowsSegmentLabels()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Groceries", Value = 500m, Percentage = 50m, Color = "#ff0000" },
            new() { Id = "2", Label = "Utilities", Value = 300m, Percentage = 30m, Color = "#00ff00" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments));

        Assert.Contains("Groceries", cut.Markup);
        Assert.Contains("Utilities", cut.Markup);
    }

    /// <summary>
    /// Verifies the legend shows segment percentages.
    /// </summary>
    [Fact]
    public void ChartLegend_ShowsPercentages()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Food", Value = 500m, Percentage = 50.5m, Color = "#ff0000" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments));

        Assert.Contains("50.5%", cut.Markup);
    }

    /// <summary>
    /// Verifies the legend applies the color to the swatch.
    /// </summary>
    [Fact]
    public void ChartLegend_AppliesColorSwatch()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Rent", Value = 1200m, Percentage = 60m, Color = "#3b82f6" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments));

        var swatch = cut.Find(".legend-color");
        Assert.Contains("#3b82f6", swatch.GetAttribute("style"));
    }

    /// <summary>
    /// Verifies clicking a legend item fires the OnItemClick callback.
    /// </summary>
    [Fact]
    public void ChartLegend_Click_FiresOnItemClick()
    {
        DonutSegmentData? clicked = null;
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Food", Value = 200m, Percentage = 40m, Color = "#ff0000" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.OnItemClick, (DonutSegmentData s) => clicked = s));

        cut.Find(".legend-item").Click();

        Assert.NotNull(clicked);
        Assert.Equal("Food", clicked.Label);
    }

    /// <summary>
    /// Verifies hovering a legend item fires the OnItemHover callback.
    /// </summary>
    [Fact]
    public void ChartLegend_MouseEnter_FiresOnItemHover()
    {
        DonutSegmentData? hovered = null;
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Transport", Value = 150m, Percentage = 30m, Color = "#00ff00" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.OnItemHover, (DonutSegmentData s) => hovered = s));

        cut.Find(".legend-item").MouseEnter();

        Assert.NotNull(hovered);
        Assert.Equal("Transport", hovered.Label);
    }

    /// <summary>
    /// Verifies mouse leave fires the OnItemHoverEnd callback.
    /// </summary>
    [Fact]
    public void ChartLegend_MouseLeave_FiresOnItemHoverEnd()
    {
        var hoverEnded = false;
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Other", Value = 50m, Percentage = 10m, Color = "#0000ff" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.OnItemHoverEnd, () => hoverEnded = true));

        cut.Find(".legend-item").MouseLeave();

        Assert.True(hoverEnded);
    }

    /// <summary>
    /// Verifies the hovered class is applied when HoveredSegmentId matches.
    /// </summary>
    [Fact]
    public void ChartLegend_AppliesHoveredClass_WhenSegmentIdMatches()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "seg-1", Label = "A", Value = 100m, Percentage = 50m, Color = "#aaa" },
            new() { Id = "seg-2", Label = "B", Value = 100m, Percentage = 50m, Color = "#bbb" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.HoveredSegmentId, "seg-1"));

        var items = cut.FindAll(".legend-item");
        Assert.Contains("hovered", items[0].ClassList);
        Assert.DoesNotContain("hovered", items[1].ClassList);
    }

    /// <summary>
    /// Verifies each legend item has an accessible aria-label.
    /// </summary>
    [Fact]
    public void ChartLegend_HasAriaLabel()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = "1", Label = "Food", Value = 250.50m, Percentage = 25m, Color = "#ff0000" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments));

        var item = cut.Find(".legend-item");
        var ariaLabel = item.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
        Assert.Contains("Food", ariaLabel);
    }

    /// <summary>
    /// Verifies the legend uses index as ID when segment Id is null.
    /// </summary>
    [Fact]
    public void ChartLegend_UsesIndex_WhenSegmentIdIsNull()
    {
        var segments = new List<DonutSegmentData>
        {
            new() { Id = null, Label = "Item", Value = 100m, Percentage = 100m, Color = "#ccc" },
        };

        var cut = Render<ChartLegend>(parameters => parameters
            .Add(p => p.Segments, segments)
            .Add(p => p.HoveredSegmentId, "0"));

        var item = cut.Find(".legend-item");
        Assert.Contains("hovered", item.ClassList);
    }

    private static List<DonutSegmentData> CreateSegments(int count)
    {
        var segments = new List<DonutSegmentData>();
        for (var i = 0; i < count; i++)
        {
            segments.Add(new DonutSegmentData
            {
                Id = i.ToString(),
                Label = $"Segment {i}",
                Value = 100m * (i + 1),
                Percentage = 100m / count,
                Color = $"#{i:D6}",
            });
        }

        return segments;
    }
}

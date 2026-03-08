// <copyright file="DonutChartSegmentTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Charts;

/// <summary>
/// Unit tests for the <see cref="DonutChartSegment"/> component.
/// </summary>
public class DonutChartSegmentTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DonutChartSegmentTests"/> class.
    /// </summary>
    public DonutChartSegmentTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <summary>
    /// Verifies the segment renders an SVG circle.
    /// </summary>
    [Fact]
    public void Renders_SvgCircle()
    {
        var cut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, CreateSegment())
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30));

        cut.Markup.ShouldContain("<circle");
    }

    /// <summary>
    /// Verifies the segment has the correct stroke color.
    /// </summary>
    [Fact]
    public void HasCorrectStrokeColor()
    {
        var segment = CreateSegment();
        segment.Data.Color = "#ff6384";

        var cut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, segment)
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30));

        cut.Markup.ShouldContain("#ff6384");
    }

    /// <summary>
    /// Verifies the segment has accessibility attributes.
    /// </summary>
    [Fact]
    public void HasAccessibilityAttributes()
    {
        var cut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, CreateSegment())
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30));

        cut.Markup.ShouldContain("role=\"button\"");
        cut.Markup.ShouldContain("tabindex=\"0\"");
    }

    /// <summary>
    /// Verifies the segment has aria-label with data.
    /// </summary>
    [Fact]
    public void HasAriaLabel()
    {
        var segment = CreateSegment();
        segment.Data.Label = "Groceries";

        var cut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, segment)
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30));

        cut.Markup.ShouldContain("aria-label");
        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies the segment uses the dash-array from calculated data.
    /// </summary>
    [Fact]
    public void UsesDashArray()
    {
        var segment = CreateSegment();
        segment.DashArray = "157 344";

        var cut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, segment)
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30));

        cut.Markup.ShouldContain("157 344");
    }

    /// <summary>
    /// Verifies hovered state increases stroke width.
    /// </summary>
    [Fact]
    public void Hovered_IncreasesStrokeWidth()
    {
        var normalCut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, CreateSegment())
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30)
            .Add(x => x.IsHovered, false));

        var hoveredCut = Render<DonutChartSegment>(p => p
            .Add(x => x.Segment, CreateSegment())
            .Add(x => x.Center, 100)
            .Add(x => x.Radius, 80)
            .Add(x => x.StrokeWidth, 30)
            .Add(x => x.IsHovered, true));

        // Hovered should have different stroke-width (30 + 4 = 34)
        hoveredCut.Markup.ShouldContain("34");
    }

    private static DonutChart.CalculatedSegment CreateSegment()
    {
        return new DonutChart.CalculatedSegment
        {
            Id = "seg-1",
            Data = new DonutSegmentData
            {
                Id = "seg-1",
                Label = "Test",
                Value = 100m,
                Percentage = 25m,
                Color = "#6b7280",
            },
            DashArray = "125 375",
            DashOffset = "0",
        };
    }
}

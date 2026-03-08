// <copyright file="ChartGridTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Shared;
using Bunit;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Charts.Shared;

/// <summary>
/// Unit tests for the <see cref="ChartGrid"/> component.
/// </summary>
public class ChartGridTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartGridTests"/> class.
    /// </summary>
    public ChartGridTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies the grid renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<ChartGrid>();

        cut.Markup.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies horizontal grid lines are rendered for Y-axis ticks.
    /// </summary>
    [Fact]
    public void RendersHorizontalLines_ForHorizontalTicks()
    {
        var ticks = new List<ChartTick>
        {
            new() { Position = 50, Label = "$100" },
            new() { Position = 150, Label = "$200" },
        };

        var cut = Render<ChartGrid>(p => p
            .Add(x => x.Left, 40)
            .Add(x => x.Right, 400)
            .Add(x => x.Top, 10)
            .Add(x => x.Bottom, 300)
            .Add(x => x.HorizontalTicks, ticks));

        cut.Markup.ShouldContain("chart-grid-line");
    }

    /// <summary>
    /// Verifies vertical grid lines are rendered for X-axis ticks.
    /// </summary>
    [Fact]
    public void RendersVerticalLines_ForVerticalTicks()
    {
        var ticks = new List<ChartTick>
        {
            new() { Position = 100, Label = "Jan" },
            new() { Position = 200, Label = "Feb" },
        };

        var cut = Render<ChartGrid>(p => p
            .Add(x => x.Left, 40)
            .Add(x => x.Right, 400)
            .Add(x => x.Top, 10)
            .Add(x => x.Bottom, 300)
            .Add(x => x.VerticalTicks, ticks));

        cut.Markup.ShouldContain("chart-grid-line");
    }

    /// <summary>
    /// Verifies no lines rendered with empty ticks.
    /// </summary>
    [Fact]
    public void EmptyTicks_NoLines()
    {
        var cut = Render<ChartGrid>(p => p
            .Add(x => x.HorizontalTicks, Array.Empty<ChartTick>())
            .Add(x => x.VerticalTicks, Array.Empty<ChartTick>()));

        cut.Markup.ShouldNotContain("chart-grid-line");
    }
}

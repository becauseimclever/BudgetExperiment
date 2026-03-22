// <copyright file="ChartAxisTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Shared;

using Bunit;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Charts.Shared;

/// <summary>
/// Unit tests for the <see cref="ChartAxis"/> component.
/// </summary>
public class ChartAxisTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAxisTests"/> class.
    /// </summary>
    public ChartAxisTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies the axis renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<ChartAxis>();

        cut.Markup.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies X-axis renders tick labels.
    /// </summary>
    [Fact]
    public void XAxis_RendersTickLabels()
    {
        var ticks = new List<ChartTick>
        {
            new() { Position = 100, Label = "Jan" },
            new() { Position = 200, Label = "Feb" },
        };

        var cut = Render<ChartAxis>(p => p
            .Add(x => x.Orientation, "x")
            .Add(x => x.Ticks, ticks)
            .Add(x => x.AxisPosition, 300));

        cut.Markup.ShouldContain("Jan");
        cut.Markup.ShouldContain("Feb");
    }

    /// <summary>
    /// Verifies Y-axis renders tick labels.
    /// </summary>
    [Fact]
    public void YAxis_RendersTickLabels()
    {
        var ticks = new List<ChartTick>
        {
            new() { Position = 50, Label = "$100" },
            new() { Position = 150, Label = "$200" },
        };

        var cut = Render<ChartAxis>(p => p
            .Add(x => x.Orientation, "y")
            .Add(x => x.Ticks, ticks)
            .Add(x => x.AxisPosition, 40));

        cut.Markup.ShouldContain("$100");
        cut.Markup.ShouldContain("$200");
    }

    /// <summary>
    /// Verifies X-axis uses middle text-anchor.
    /// </summary>
    [Fact]
    public void XAxis_UsesMiddleTextAnchor()
    {
        var ticks = new List<ChartTick> { new() { Position = 100, Label = "Jan" } };

        var cut = Render<ChartAxis>(p => p
            .Add(x => x.Orientation, "x")
            .Add(x => x.Ticks, ticks)
            .Add(x => x.AxisPosition, 300));

        cut.Markup.ShouldContain("text-anchor=\"middle\"");
    }

    /// <summary>
    /// Verifies Y-axis uses end text-anchor.
    /// </summary>
    [Fact]
    public void YAxis_UsesEndTextAnchor()
    {
        var ticks = new List<ChartTick> { new() { Position = 100, Label = "$100" } };

        var cut = Render<ChartAxis>(p => p
            .Add(x => x.Orientation, "y")
            .Add(x => x.Ticks, ticks)
            .Add(x => x.AxisPosition, 40));

        cut.Markup.ShouldContain("text-anchor=\"end\"");
    }

    /// <summary>
    /// Verifies empty ticks renders no text elements.
    /// </summary>
    [Fact]
    public void EmptyTicks_RendersNoText()
    {
        var cut = Render<ChartAxis>(p => p
            .Add(x => x.Ticks, Array.Empty<ChartTick>()));

        cut.Markup.ShouldNotContain("<text");
    }
}

// <copyright file="RadialGaugeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the RadialGauge component.
/// </summary>
public class RadialGaugeTests : BunitContext
{
    [Fact]
    public void RadialGauge_Renders_EmptyState_WhenMaxZero()
    {
        // Act
        var cut = Render<RadialGauge>(parameters => parameters
            .Add(p => p.Value, 0m)
            .Add(p => p.MaxValue, 0m));

        // Assert
        var empty = cut.Find(".radial-gauge-empty");
        Assert.NotNull(empty);
    }

    [Fact]
    public void RadialGauge_Renders_Circles_WhenDataProvided()
    {
        // Act
        var cut = Render<RadialGauge>(parameters => parameters
            .Add(p => p.Value, 60m)
            .Add(p => p.MaxValue, 100m));

        // Assert
        var circles = cut.FindAll("circle");
        Assert.Equal(2, circles.Count);
    }

    [Fact]
    public void RadialGauge_Uses_AriaLabel()
    {
        // Act
        var cut = Render<RadialGauge>(parameters => parameters
            .Add(p => p.Value, 20m)
            .Add(p => p.MaxValue, 100m)
            .Add(p => p.AriaLabel, "Gauge status"));

        // Assert
        var gauge = cut.Find(".radial-gauge");
        Assert.Equal("Gauge status", gauge.GetAttribute("aria-label"));
    }

    [Fact]
    public void RadialGauge_Calculates_DashOffset()
    {
        // Arrange
        const int size = 120;
        const int strokeWidth = 10;
        var value = 50m;
        var max = 100m;

        // Act
        var cut = Render<RadialGauge>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.MaxValue, max)
            .Add(p => p.Size, size)
            .Add(p => p.StrokeWidth, strokeWidth));

        // Assert
        var progress = cut.Find(".radial-gauge-progress");
        var dashOffset = double.Parse(progress.GetAttribute("stroke-dashoffset") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

        var radius = (size - strokeWidth) / 2d - 2d;
        var circumference = 2 * Math.PI * radius;
        var expected = circumference * (1 - (double)(value / max));

        Assert.InRange(dashOffset, expected - 0.2, expected + 0.2);
    }
}

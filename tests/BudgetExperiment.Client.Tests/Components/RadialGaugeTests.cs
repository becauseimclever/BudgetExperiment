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
}

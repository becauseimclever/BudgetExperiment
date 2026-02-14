// <copyright file="SparkLineTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the SparkLine component.
/// </summary>
public class SparkLineTests : BunitContext
{
    [Fact]
    public void SparkLine_Renders_EmptyState_WhenNoValues()
    {
        // Act
        var cut = Render<SparkLine>(parameters => parameters
            .Add(p => p.Values, new List<decimal>()));

        // Assert
        var empty = cut.Find(".sparkline-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data", empty.TextContent);
    }

    [Fact]
    public void SparkLine_Renders_Polyline_WhenValuesProvided()
    {
        // Arrange
        var values = new List<decimal> { 10m, 20m, 15m };

        // Act
        var cut = Render<SparkLine>(parameters => parameters
            .Add(p => p.Values, values));

        // Assert
        var line = cut.Find("polyline.sparkline-line");
        Assert.NotNull(line);
    }

    [Fact]
    public void SparkLine_Uses_Trend_Color_WhenEnabled()
    {
        // Arrange
        var values = new List<decimal> { 10m, 30m };

        // Act
        var cut = Render<SparkLine>(parameters => parameters
            .Add(p => p.Values, values)
            .Add(p => p.ColorByTrend, true)
            .Add(p => p.PositiveColor, "#00ff00"));

        // Assert
        var line = cut.Find("polyline.sparkline-line");
        Assert.Equal("#00ff00", line.GetAttribute("stroke"));
    }

    [Fact]
    public void SparkLine_Renders_Endpoints_WhenEnabled()
    {
        // Arrange
        var values = new List<decimal> { 10m, 30m };

        // Act
        var cut = Render<SparkLine>(parameters => parameters
            .Add(p => p.Values, values)
            .Add(p => p.ShowEndpoints, true));

        // Assert
        var endpoints = cut.FindAll("circle.sparkline-endpoint");
        Assert.Equal(2, endpoints.Count);
    }
}

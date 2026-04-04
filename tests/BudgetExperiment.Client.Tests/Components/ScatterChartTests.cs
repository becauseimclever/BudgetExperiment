// <copyright file="ScatterChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.ScatterChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the ScatterChart component.
/// </summary>
public class ScatterChartTests : BunitContext
{
    private static readonly DateOnly _today = new DateOnly(2026, 1, 15);

    [Fact]
    public void ScatterChart_Renders_EmptyState_WhenNoPoints()
    {
        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, new List<ScatterDataPoint>()));

        // Assert
        var empty = cut.Find(".scatter-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void ScatterChart_Renders_SVG_WhenPointsProvided()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 150m, "Groceries"),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points));

        // Assert
        var svg = cut.Find("svg.scatter-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void ScatterChart_Renders_OneCircle_PerPoint()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 150m, "Groceries"),
            new(_today.AddDays(1), 75m, "Transport"),
            new(_today.AddDays(2), 300m, "Dining"),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points));

        // Assert
        var circles = cut.FindAll("circle.scatter-point");
        Assert.Equal(3, circles.Count);
    }

    [Fact]
    public void ScatterChart_OutlierPoint_HasOutlierClass()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 5000m, "Other", IsOutlier: true),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points));

        // Assert — outlier circle must carry both the base class and the outlier class
        var circles = cut.FindAll("circle.scatter-point");
        Assert.Single(circles);
        Assert.Contains("scatter-point-outlier", circles[0].ClassName ?? string.Empty);
    }

    [Fact]
    public void ScatterChart_NonOutlier_DoesNotHaveOutlierClass()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 150m, "Groceries", IsOutlier: false),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points));

        // Assert
        var circles = cut.FindAll("circle.scatter-point");
        Assert.Single(circles);
        Assert.DoesNotContain("scatter-point-outlier", circles[0].ClassName ?? string.Empty);
    }

    [Fact]
    public void ScatterChart_WithShowOutlierMarkers_False_OutlierHasNoSpecialClass()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 5000m, "Other", IsOutlier: true),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points)
            .Add(p => p.ShowOutlierMarkers, false));

        // Assert — when ShowOutlierMarkers is false, outlier renders as a plain scatter point
        var circles = cut.FindAll("circle.scatter-point");
        Assert.Single(circles);
        Assert.DoesNotContain("scatter-point-outlier", circles[0].ClassName ?? string.Empty);
    }

    [Fact]
    public void ScatterChart_UsesAriaLabel()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 100m, "Groceries"),
        };

        const string label = "Q1 transaction scatter plot";

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points)
            .Add(p => p.AriaLabel, label));

        // Assert
        var container = cut.Find(".scatter-chart");
        Assert.Equal(label, container.GetAttribute("aria-label"));
    }

    [Fact]
    public void ScatterChart_RendersAxes()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 100m, "Groceries"),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points));

        // Assert — at least one axis element must be present when data is provided
        var axes = cut.FindAll(".scatter-axis");
        Assert.True(axes.Count >= 1);
    }

    // NOTE: The following two tests cover the AnimationsEnabled parameter on ScatterChart (Slice 7 — visual polish).
    [Fact]
    public void ScatterChart_HasNoAnimations_WhenAnimationsDisabled()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 100m, "Groceries"),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points)
            .Add(p => p.AnimationsEnabled, false));

        // Assert — outer div must carry the no-animation CSS class when animations are disabled
        var container = cut.Find(".scatter-chart");
        Assert.Contains("chart-no-animation", container.ClassName ?? string.Empty);
    }

    [Fact]
    public void ScatterChart_HasAnimations_WhenAnimationsEnabled()
    {
        // Arrange
        var points = new List<ScatterDataPoint>
        {
            new(_today, 100m, "Groceries"),
        };

        // Act
        var cut = Render<ScatterChart>(parameters => parameters
            .Add(p => p.Points, points)
            .Add(p => p.AnimationsEnabled, true));

        // Assert — when animations are enabled, the no-animation class must not be present
        var container = cut.Find(".scatter-chart");
        Assert.DoesNotContain("chart-no-animation", container.ClassName ?? string.Empty);
    }
}

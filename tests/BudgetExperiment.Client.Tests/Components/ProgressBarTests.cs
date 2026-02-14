// <copyright file="ProgressBarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the ProgressBar component.
/// </summary>
public class ProgressBarTests : BunitContext
{
    [Fact]
    public void ProgressBar_Renders_Progress_WithPercent()
    {
        // Act
        var cut = Render<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 50m)
            .Add(p => p.MaxValue, 100m)
            .Add(p => p.Label, "Budget"));

        // Assert
        var fill = cut.Find(".progress-bar-fill");
        Assert.Contains("width: 50", fill.GetAttribute("style"));

        var label = cut.Find(".progress-bar-text");
        Assert.Contains("Budget", label.TextContent);
    }

    [Fact]
    public void ProgressBar_Uses_AriaLabel()
    {
        // Act
        var cut = Render<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 10m)
            .Add(p => p.MaxValue, 100m)
            .Add(p => p.AriaLabel, "Progress status"));

        // Assert
        var bar = cut.Find(".progress-bar");
        Assert.Equal("Progress status", bar.GetAttribute("aria-label"));
    }

    [Fact]
    public void ProgressBar_Uses_Threshold_Color()
    {
        // Arrange
        var thresholds = new List<ThresholdColor>
        {
            new() { MinPercent = 0m, Color = "#00ff00", Label = "On track" },
            new() { MinPercent = 80m, Color = "#ff0000", Label = "Over" },
        };

        // Act
        var cut = Render<ProgressBar>(parameters => parameters
            .Add(p => p.Value, 90m)
            .Add(p => p.MaxValue, 100m)
            .Add(p => p.Thresholds, thresholds));

        // Assert
        var fill = cut.Find(".progress-bar-fill");
        Assert.Contains("#ff0000", fill.GetAttribute("style"));
    }
}

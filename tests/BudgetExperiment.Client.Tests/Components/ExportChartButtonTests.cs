// <copyright file="ExportChartButtonTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.ExportChartButton (Razor component)
// They define the expected parameter API and rendered HTML contract.
// IMPORTANT: Do NOT test the actual JS export functionality — that is browser-only.
// Only the DOM structure of the button is verified here.
using BudgetExperiment.Client.Components.Charts;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the ExportChartButton component.
/// </summary>
public class ExportChartButtonTests : BunitContext
{
    [Fact]
    public void ExportChartButton_Renders_Button_WhenVisible()
    {
        // Act
        var cut = Render<ExportChartButton>(parameters => parameters
            .Add(p => p.Visible, true));

        // Assert
        var button = cut.Find("button.export-chart-btn");
        Assert.NotNull(button);
    }

    [Fact]
    public void ExportChartButton_DoesNotRender_WhenNotVisible()
    {
        // Act
        var cut = Render<ExportChartButton>(parameters => parameters
            .Add(p => p.Visible, false));

        // Assert — no output when not visible
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void ExportChartButton_Button_HasTypeButton()
    {
        // Act
        var cut = Render<ExportChartButton>(parameters => parameters
            .Add(p => p.Visible, true));

        // Assert
        var button = cut.Find("button.export-chart-btn");
        Assert.Equal("button", button.GetAttribute("type"));
    }

    [Fact]
    public void ExportChartButton_Button_HasAriaLabel()
    {
        // Act
        var cut = Render<ExportChartButton>(parameters => parameters
            .Add(p => p.Visible, true));

        // Assert
        var button = cut.Find("button.export-chart-btn");
        var ariaLabel = button.GetAttribute("aria-label");
        Assert.False(string.IsNullOrWhiteSpace(ariaLabel));
    }

    [Fact]
    public void ExportChartButton_Renders_WithDefaultVisibility()
    {
        // Act — Visible parameter not set; default is true
        var cut = Render<ExportChartButton>();

        // Assert
        var button = cut.Find("button.export-chart-btn");
        Assert.NotNull(button);
    }
}

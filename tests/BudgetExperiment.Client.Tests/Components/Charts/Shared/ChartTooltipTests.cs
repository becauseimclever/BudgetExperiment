// <copyright file="ChartTooltipTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Shared;

using Bunit;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Charts.Shared;

/// <summary>
/// Unit tests for the <see cref="ChartTooltip"/> component.
/// </summary>
public class ChartTooltipTests : BunitContext
{
    /// <summary>
    /// Verifies nothing is rendered when Visible is false.
    /// </summary>
    [Fact]
    public void RendersNothing_WhenNotVisible()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, false)
            .Add(x => x.Title, "Test")
            .Add(x => x.Value, "$100"));

        cut.Markup.ShouldNotContain("chart-tooltip");
    }

    /// <summary>
    /// Verifies the tooltip renders when Visible is true.
    /// </summary>
    [Fact]
    public void RendersTooltip_WhenVisible()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Value, "$100"));

        cut.Find(".chart-tooltip").ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies the value is always displayed.
    /// </summary>
    [Fact]
    public void ShowsValue()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Value, "$250.00"));

        cut.Find(".chart-tooltip-value").TextContent.ShouldBe("$250.00");
    }

    /// <summary>
    /// Verifies the title is rendered when provided.
    /// </summary>
    [Fact]
    public void ShowsTitle_WhenProvided()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Title, "January")
            .Add(x => x.Value, "$100"));

        cut.Find(".chart-tooltip-label").TextContent.ShouldBe("January");
    }

    /// <summary>
    /// Verifies the title is not rendered when null.
    /// </summary>
    [Fact]
    public void HidesTitle_WhenNull()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Title, (string?)null)
            .Add(x => x.Value, "$100"));

        cut.FindAll(".chart-tooltip-label").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the title is not rendered when empty or whitespace.
    /// </summary>
    [Fact]
    public void HidesTitle_WhenWhitespace()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Title, "  ")
            .Add(x => x.Value, "$100"));

        cut.FindAll(".chart-tooltip-label").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the series label is rendered when provided.
    /// </summary>
    [Fact]
    public void ShowsSeries_WhenProvided()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Series, "Groceries")
            .Add(x => x.Value, "$100"));

        cut.Find(".chart-tooltip-series").TextContent.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies the series label is not rendered when null.
    /// </summary>
    [Fact]
    public void HidesSeries_WhenNull()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Series, (string?)null)
            .Add(x => x.Value, "$100"));

        cut.FindAll(".chart-tooltip-series").ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the tooltip has role=status for accessibility.
    /// </summary>
    [Fact]
    public void HasRoleStatus()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Value, "$100"));

        cut.Find("[role='status']").ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies all sections render when all parameters provided.
    /// </summary>
    [Fact]
    public void RendersAllSections_WhenAllParametersProvided()
    {
        var cut = Render<ChartTooltip>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.Title, "March 2026")
            .Add(x => x.Series, "Utilities")
            .Add(x => x.Value, "$150.75"));

        cut.Find(".chart-tooltip-label").TextContent.ShouldBe("March 2026");
        cut.Find(".chart-tooltip-series").TextContent.ShouldBe("Utilities");
        cut.Find(".chart-tooltip-value").TextContent.ShouldBe("$150.75");
    }
}

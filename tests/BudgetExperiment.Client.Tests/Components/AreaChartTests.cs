// <copyright file="AreaChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;
using BudgetExperiment.Client.Components.Charts;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the AreaChart component.
/// </summary>
public class AreaChartTests : BunitContext
{
    [Fact]
    public void AreaChart_Renders_Area_WhenDataProvided()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
            new() { Label = "Feb", Value = 400m },
        };

        // Act
        var cut = Render<AreaChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var areaPaths = cut.FindAll("path.line-area");
        Assert.Single(areaPaths);
    }

    [Fact]
    public void AreaChart_Uses_AriaLabel()
    {
        // Arrange
        var data = new List<LineData>
        {
            new() { Label = "Jan", Value = 200m },
        };

        // Act
        var cut = Render<AreaChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.AriaLabel, "Area trend"));

        // Assert
        var svg = cut.Find("svg.line-chart");
        Assert.Equal("Area trend", svg.GetAttribute("aria-label"));
    }
}

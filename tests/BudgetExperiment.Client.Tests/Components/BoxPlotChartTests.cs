// <copyright file="BoxPlotChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.BoxPlotChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the BoxPlotChart component.
/// </summary>
public class BoxPlotChartTests : BunitContext
{
    [Fact]
    public void BoxPlotChart_Renders_EmptyState_WhenNoDistributions()
    {
        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, new List<BoxPlotSummary>()));

        // Assert
        var empty = cut.Find(".box-plot-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void BoxPlotChart_Renders_SVG_WhenDataProvided()
    {
        // Arrange — one distribution with no outliers
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", Minimum: 100m, Q1: 200m, Median: 300m, Q3: 400m, Maximum: 500m, Outliers: []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert
        var svg = cut.Find("svg.box-plot-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void BoxPlotChart_Renders_OneBoxPerDistribution()
    {
        // Arrange — 3 distributions, no outliers
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 80m, 200m, 310m, 420m, 520m, []),
            new("Dining", 50m, 100m, 150m, 250m, 350m, []),
            new("Transport", 30m, 60m, 90m, 130m, 180m, []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert
        var boxes = cut.FindAll("rect.box-plot-box");
        Assert.Equal(3, boxes.Count);
    }

    [Fact]
    public void BoxPlotChart_Renders_MedianLinePerDistribution()
    {
        // Arrange — 2 distributions
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 100m, 200m, 300m, 400m, 500m, []),
            new("Dining", 50m, 100m, 150m, 200m, 280m, []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert
        var medians = cut.FindAll("line.box-plot-median");
        Assert.Equal(2, medians.Count);
    }

    [Fact]
    public void BoxPlotChart_Renders_WhiskersPerDistribution()
    {
        // Arrange — 1 distribution → whisker lines (min to Q1, Q3 to max)
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 80m, 200m, 300m, 420m, 520m, []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert — at least one whisker line per distribution
        var whiskers = cut.FindAll("line.box-plot-whisker");
        Assert.NotEmpty(whiskers);
    }

    [Fact]
    public void BoxPlotChart_Renders_OutlierCircles_WhenOutliersExist()
    {
        // Arrange — one distribution with 2 outliers
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 100m, 200m, 300m, 400m, 500m, [600m, 750m]),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert
        var outliers = cut.FindAll("circle.box-plot-outlier");
        Assert.Equal(2, outliers.Count);
    }

    [Fact]
    public void BoxPlotChart_Renders_NoOutlierCircles_WhenNoOutliers()
    {
        // Arrange — distribution with empty Outliers list
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 100m, 200m, 300m, 400m, 500m, []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert — no outlier circles rendered
        var outliers = cut.FindAll("circle.box-plot-outlier");
        Assert.Empty(outliers);
    }

    [Fact]
    public void BoxPlotChart_Renders_LabelPerDistribution()
    {
        // Arrange — 2 distributions
        var distributions = new List<BoxPlotSummary>
        {
            new("Groceries", 100m, 200m, 300m, 400m, 500m, []),
            new("Dining", 50m, 100m, 150m, 200m, 280m, []),
        };

        // Act
        var cut = Render<BoxPlotChart>(parameters => parameters
            .Add(p => p.Distributions, distributions));

        // Assert — one label per distribution
        var labels = cut.FindAll(".box-plot-label");
        Assert.Equal(2, labels.Count);
    }
}

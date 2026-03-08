// <copyright file="ReportWidgetConfigDefinitionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ReportWidgetConfigDefinition"/>.
/// </summary>
public class ReportWidgetConfigDefinitionTests
{
    /// <summary>
    /// Verifies CreateDefault for chart type.
    /// </summary>
    [Fact]
    public void CreateDefault_Chart_ReturnsChartConfig()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault("chart");

        config.ReportType.ShouldBe("spending-by-category");
        config.DateRangePreset.ShouldBe("this-month");
        config.ShowValues.ShouldBeTrue();
        config.ShowLabels.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies CreateDefault for table type.
    /// </summary>
    [Fact]
    public void CreateDefault_Table_ReturnsTableConfig()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault("table");

        config.ReportType.ShouldBe("transactions");
        config.Columns.Count.ShouldBeGreaterThan(0);
        config.Columns.ShouldContain("date");
        config.Columns.ShouldContain("amount");
    }

    /// <summary>
    /// Verifies CreateDefault for summary type.
    /// </summary>
    [Fact]
    public void CreateDefault_Summary_ReturnsSummaryConfig()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault("summary");

        config.ReportType.ShouldBe("summary");
        config.Metric.ShouldBe("net");
        config.Comparison.ShouldBe("previous-period");
    }

    /// <summary>
    /// Verifies CreateDefault for unknown type falls back to summary.
    /// </summary>
    [Fact]
    public void CreateDefault_UnknownType_FallsBackToSummary()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault("unknown");

        config.ReportType.ShouldBe("summary");
    }

    /// <summary>
    /// Verifies CreateDefault handles null type.
    /// </summary>
    [Fact]
    public void CreateDefault_Null_FallsBackToSummary()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault(null!);

        config.ReportType.ShouldBe("summary");
    }

    /// <summary>
    /// Verifies CreateDefault trims and handles case.
    /// </summary>
    [Fact]
    public void CreateDefault_TrimsAndIgnoresCase()
    {
        var config = ReportWidgetConfigDefinition.CreateDefault("  CHART  ");

        config.ReportType.ShouldBe("spending-by-category");
    }

    /// <summary>
    /// Verifies default constructor has expected defaults.
    /// </summary>
    [Fact]
    public void Constructor_HasExpectedDefaults()
    {
        var config = new ReportWidgetConfigDefinition();

        config.ReportType.ShouldBe("summary");
        config.DateRangePreset.ShouldBe("last-30-days");
        config.Orientation.ShouldBe("vertical");
        config.ShowValues.ShouldBeTrue();
        config.ShowLabels.ShouldBeTrue();
        config.Series.ShouldNotBeNull();
        config.Columns.ShouldNotBeNull();
    }
}

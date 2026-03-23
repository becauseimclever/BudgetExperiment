// <copyright file="CustomReportLayoutDefinitionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Models;

/// <summary>
/// Unit tests for <see cref="CustomReportLayoutDefinition"/>.
/// </summary>
public class CustomReportLayoutDefinitionTests
{
    /// <summary>
    /// Verifies default constructor creates empty widget list.
    /// </summary>
    [Fact]
    public void Constructor_CreatesEmptyWidgetsList()
    {
        var layout = new CustomReportLayoutDefinition();

        layout.Widgets.ShouldNotBeNull();
        layout.Widgets.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies default constructor creates grid.
    /// </summary>
    [Fact]
    public void Constructor_CreatesDefaultGrid()
    {
        var layout = new CustomReportLayoutDefinition();

        layout.Grid.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies Normalize creates grid if null.
    /// </summary>
    [Fact]
    public void Normalize_CreatesGrid_WhenNull()
    {
        var layout = new CustomReportLayoutDefinition { Grid = null! };

        layout.Normalize();

        layout.Grid.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies Normalize applies defaults to widgets.
    /// </summary>
    [Fact]
    public void Normalize_AppliesDefaultsToWidgets()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = "summary",
            Title = "Test Widget",
        };
        layout.Widgets.Add(widget);

        layout.Normalize();

        widget.Constraints.ShouldNotBeNull();
        widget.Config.ShouldNotBeNull();
        widget.Layouts.ShouldNotBeNull();
        widget.Layouts.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies Normalize sets up all breakpoint layouts.
    /// </summary>
    [Fact]
    public void Normalize_SetsAllBreakpointLayouts()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = "chart",
        };
        layout.Widgets.Add(widget);

        layout.Normalize();

        widget.Layouts.ContainsKey("lg").ShouldBeTrue();
        widget.Layouts.ContainsKey("md").ShouldBeTrue();
        widget.Layouts.ContainsKey("sm").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies AddWidget adds widget to the list.
    /// </summary>
    [Fact]
    public void AddWidget_AddsToWidgetsList()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = "summary",
            Title = "Revenue",
        };

        layout.AddWidget(widget);

        layout.Widgets.Count.ShouldBe(1);
        layout.Widgets[0].ShouldBe(widget);
    }

    /// <summary>
    /// Verifies AddWidget applies defaults to the widget.
    /// </summary>
    [Fact]
    public void AddWidget_AppliesDefaults()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = "chart",
        };

        layout.AddWidget(widget);

        widget.Constraints.ShouldNotBeNull();
        widget.Config.ShouldNotBeNull();
        widget.Layouts.ContainsKey("lg").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies AddWidget stacks second widget below first.
    /// </summary>
    [Fact]
    public void AddWidget_StacksWidgetsVertically()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget1 = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };
        var widget2 = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "chart" };

        layout.AddWidget(widget1);
        layout.AddWidget(widget2);

        var firstY = widget1.Layouts["lg"].Y;
        var firstHeight = widget1.Layouts["lg"].Height;
        var secondY = widget2.Layouts["lg"].Y;
        secondY.ShouldBeGreaterThanOrEqualTo(firstY + firstHeight);
    }

    /// <summary>
    /// Verifies UpdateWidgetLayout updates existing widget.
    /// </summary>
    [Fact]
    public void UpdateWidgetLayout_UpdatesExistingWidget()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };
        layout.AddWidget(widget);

        var newPosition = new ReportWidgetLayoutPosition
        {
            X = 3,
            Y = 2,
            Width = 6,
            Height = 4,
        };

        layout.UpdateWidgetLayout(widget.Id, "lg", newPosition);

        widget.Layouts["lg"].X.ShouldBe(3);
        widget.Layouts["lg"].Width.ShouldBe(6);
    }

    /// <summary>
    /// Verifies UpdateWidgetLayout ignores unknown widget ID.
    /// </summary>
    [Fact]
    public void UpdateWidgetLayout_IgnoresUnknownWidgetId()
    {
        var layout = new CustomReportLayoutDefinition();

        layout.UpdateWidgetLayout(Guid.NewGuid(), "lg", new ReportWidgetLayoutPosition
        {
            X = 1,
            Y = 1,
            Width = 4,
            Height = 4,
        });

        layout.Widgets.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies UpdateWidgetLayout clamps width to grid columns.
    /// </summary>
    [Fact]
    public void UpdateWidgetLayout_ClampsToGridColumns()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };
        layout.AddWidget(widget);

        var oversized = new ReportWidgetLayoutPosition
        {
            X = 1,
            Y = 1,
            Width = 99,
            Height = 99,
        };

        layout.UpdateWidgetLayout(widget.Id, "lg", oversized);

        widget.Layouts["lg"].Width.ShouldBeLessThanOrEqualTo(12);
    }

    /// <summary>
    /// Verifies Normalize with multiple widgets tracks next row.
    /// </summary>
    [Fact]
    public void Normalize_TracksNextRow_WithMultipleWidgets()
    {
        var layout = new CustomReportLayoutDefinition();
        var w1 = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };
        var w2 = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "chart" };
        var w3 = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "table" };
        layout.Widgets.AddRange([w1, w2, w3]);

        layout.Normalize();

        w1.Layouts["lg"].ShouldNotBeNull();
        w2.Layouts["lg"].ShouldNotBeNull();
        w3.Layouts["lg"].ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies AddWidget creates config for chart type.
    /// </summary>
    [Fact]
    public void AddWidget_ChartType_CreatesChartConfig()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "chart" };

        layout.AddWidget(widget);

        widget.Config.ShouldNotBeNull();
        widget.Config!.ReportType.ShouldBe("spending-by-category");
    }

    /// <summary>
    /// Verifies AddWidget creates config for table type.
    /// </summary>
    [Fact]
    public void AddWidget_TableType_CreatesTableConfig()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "table" };

        layout.AddWidget(widget);

        widget.Config.ShouldNotBeNull();
        widget.Config!.Columns.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies widget constraints have expected defaults.
    /// </summary>
    [Fact]
    public void AddWidget_SetsDefaultConstraints()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };

        layout.AddWidget(widget);

        widget.Constraints!.MinWidth.ShouldBe(2);
        widget.Constraints.MinHeight.ShouldBe(2);
        widget.Constraints.MaxWidth.ShouldBe(12);
        widget.Constraints.MaxHeight.ShouldBe(12);
    }

    /// <summary>
    /// Verifies UpdateWidgetLayout clamps position to minimum Y.
    /// </summary>
    [Fact]
    public void UpdateWidgetLayout_ClampsMinimumY()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };
        layout.AddWidget(widget);

        layout.UpdateWidgetLayout(widget.Id, "lg", new ReportWidgetLayoutPosition
        {
            X = 1,
            Y = -5,
            Width = 4,
            Height = 4,
        });

        widget.Layouts["lg"].Y.ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies sm breakpoint gets smaller default width.
    /// </summary>
    [Fact]
    public void AddWidget_SmBreakpoint_FitsSmColumns()
    {
        var layout = new CustomReportLayoutDefinition();
        var widget = new ReportWidgetDefinition { Id = Guid.NewGuid(), Type = "summary" };

        layout.AddWidget(widget);

        widget.Layouts["sm"].Width.ShouldBeLessThanOrEqualTo(4);
    }
}

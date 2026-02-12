// <copyright file="WidgetConfigPanelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Models;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the WidgetConfigPanel component.
/// </summary>
public class WidgetConfigPanelTests : BunitContext
{
    [Fact]
    public void WidgetConfigPanel_Renders_Empty_State_WhenNoWidget()
    {
        // Act
        var cut = Render<WidgetConfigPanel>(parameters => parameters
            .Add(p => p.Widget, null));

        // Assert
        Assert.Contains("Select a widget", cut.Markup);
    }

    [Fact]
    public void WidgetConfigPanel_Shows_Title_Validation_Error()
    {
        // Arrange
        var widget = CreateWidget("summary", "   ");

        // Act
        var cut = Render<WidgetConfigPanel>(parameters => parameters
            .Add(p => p.Widget, widget));

        // Assert
        Assert.Contains("Title is required", cut.Markup);
    }

    [Fact]
    public void WidgetConfigPanel_Trims_Title_On_Blur()
    {
        // Arrange
        var widget = CreateWidget("summary", "  Net Summary  ");

        var cut = Render<WidgetConfigPanel>(parameters => parameters
            .Add(p => p.Widget, widget));

        // Act
        var input = cut.Find("#widget-title");
        input.Blur();

        // Assert
        Assert.Equal("Net Summary", widget.Title);
    }

    [Fact]
    public void WidgetConfigPanel_Shows_Chart_Options_WhenChartWidget()
    {
        // Arrange
        var widget = CreateWidget("chart", "Chart");

        // Act
        var cut = Render<WidgetConfigPanel>(parameters => parameters
            .Add(p => p.Widget, widget));

        // Assert
        Assert.NotEmpty(cut.FindAll("#widget-orientation"));
    }

    [Fact]
    public void WidgetConfigPanel_Toggles_Table_Columns()
    {
        // Arrange
        var widget = CreateWidget("table", "Table");

        var cut = Render<WidgetConfigPanel>(parameters => parameters
            .Add(p => p.Widget, widget));

        // Act
        var checkbox = cut.Find("input[value='notes']");
        checkbox.Change(true);

        // Assert
        Assert.Contains("notes", widget.Config!.Columns);
    }

    private static ReportWidgetDefinition CreateWidget(string type, string title)
    {
        return new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Config = ReportWidgetConfigDefinition.CreateDefault(type),
            Constraints = new ReportWidgetConstraints
            {
                MinWidth = 2,
                MinHeight = 2,
                MaxWidth = 12,
                MaxHeight = 12,
            },
        };
    }
}

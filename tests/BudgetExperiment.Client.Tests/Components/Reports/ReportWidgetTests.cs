// <copyright file="ReportWidgetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Models;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the ReportWidget component.
/// </summary>
public class ReportWidgetTests : BunitContext
{
    [Fact]
    public void ReportWidget_Renders_Title_And_Type()
    {
        // Arrange
        var widget = CreateWidget();

        // Act
        var cut = Render<ReportWidget>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.LayoutLg, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutMd, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutSm, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 }));

        // Assert
        Assert.Contains("Summary Card", cut.Markup);
        Assert.Contains("summary", cut.Markup);
    }

    [Fact]
    public void ReportWidget_Adds_Selected_Class_WhenSelected()
    {
        // Arrange
        var widget = CreateWidget();

        // Act
        var cut = Render<ReportWidget>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.IsSelected, true)
            .Add(p => p.LayoutLg, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutMd, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutSm, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 }));

        // Assert
        var root = cut.Find(".report-widget");
        Assert.Contains("is-selected", root.ClassList);
    }

    [Fact]
    public void ReportWidget_Invokes_Select_On_Header_Click()
    {
        // Arrange
        var widget = CreateWidget();
        var selectedId = Guid.Empty;

        // Act
        var cut = Render<ReportWidget>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.OnSelect, EventCallback.Factory.Create<Guid>(this, id => selectedId = id))
            .Add(p => p.LayoutLg, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutMd, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutSm, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 }));

        cut.Find(".report-widget-header").Click();

        // Assert
        Assert.Equal(widget.Id, selectedId);
    }

    [Fact]
    public void ReportWidget_Invokes_Duplicate_Action()
    {
        // Arrange
        var widget = CreateWidget();
        var duplicateId = Guid.Empty;

        // Act
        var cut = Render<ReportWidget>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.OnDuplicate, EventCallback.Factory.Create<Guid>(this, id => duplicateId = id))
            .Add(p => p.LayoutLg, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutMd, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutSm, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 }));

        cut.Find(".report-widget-action").Click();

        // Assert
        Assert.Equal(widget.Id, duplicateId);
    }

    [Fact]
    public void ReportWidget_Invokes_Remove_Action()
    {
        // Arrange
        var widget = CreateWidget();
        var removeId = Guid.Empty;

        // Act
        var cut = Render<ReportWidget>(parameters => parameters
            .Add(p => p.Widget, widget)
            .Add(p => p.OnRemove, EventCallback.Factory.Create<Guid>(this, id => removeId = id))
            .Add(p => p.LayoutLg, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutMd, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 })
            .Add(p => p.LayoutSm, new ReportWidgetLayoutPosition { X = 1, Y = 1, Width = 4, Height = 4 }));

        var buttons = cut.FindAll(".report-widget-action");
        buttons[^1].Click();

        // Assert
        Assert.Equal(widget.Id, removeId);
    }

    private static ReportWidgetDefinition CreateWidget()
    {
        return new ReportWidgetDefinition
        {
            Id = Guid.NewGuid(),
            Type = "summary",
            Title = "Summary Card",
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

// <copyright file="WidgetPaletteTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the <see cref="WidgetPalette"/> component.
/// </summary>
public class WidgetPaletteTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetPaletteTests"/> class.
    /// </summary>
    public WidgetPaletteTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// Verifies the palette renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<WidgetPalette>();

        cut.Markup.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies palette items display their titles.
    /// </summary>
    [Fact]
    public void ShowsItemTitles()
    {
        var items = new List<ReportWidgetPaletteItem>
        {
            new() { Type = "chart", Title = "Spending Chart" },
            new() { Type = "table", Title = "Transaction Table" },
        };

        var cut = Render<WidgetPalette>(p => p.Add(x => x.Items, items));

        cut.Markup.ShouldContain("Spending Chart");
        cut.Markup.ShouldContain("Transaction Table");
    }

    /// <summary>
    /// Verifies palette items display descriptions when provided.
    /// </summary>
    [Fact]
    public void ShowsItemDescriptions()
    {
        var items = new List<ReportWidgetPaletteItem>
        {
            new() { Type = "chart", Title = "Chart", Description = "Visual spending breakdown" },
        };

        var cut = Render<WidgetPalette>(p => p.Add(x => x.Items, items));

        cut.Markup.ShouldContain("Visual spending breakdown");
    }

    /// <summary>
    /// Verifies items are draggable.
    /// </summary>
    [Fact]
    public void Items_AreDraggable()
    {
        var items = new List<ReportWidgetPaletteItem>
        {
            new() { Type = "chart", Title = "Chart" },
        };

        var cut = Render<WidgetPalette>(p => p.Add(x => x.Items, items));

        cut.Markup.ShouldContain("draggable");
    }

    /// <summary>
    /// Verifies empty palette renders without errors.
    /// </summary>
    [Fact]
    public void EmptyItems_RendersWithoutErrors()
    {
        var cut = Render<WidgetPalette>(p => p
            .Add(x => x.Items, Array.Empty<ReportWidgetPaletteItem>()));

        cut.Markup.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies the palette has proper ARIA role.
    /// </summary>
    [Fact]
    public void HasListRole()
    {
        var items = new List<ReportWidgetPaletteItem>
        {
            new() { Type = "chart", Title = "Chart" },
        };

        var cut = Render<WidgetPalette>(p => p.Add(x => x.Items, items));

        cut.Markup.ShouldContain("role=\"list\"");
    }
}

// <copyright file="RulesToolbarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="RulesToolbar"/> component.
/// </summary>
public sealed class RulesToolbarTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RulesToolbarTests"/> class.
    /// </summary>
    public RulesToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <summary>
    /// Verifies that the search input is rendered.
    /// </summary>
    [Fact]
    public void RendersSearchInput()
    {
        var cut = RenderToolbar();

        var input = cut.Find("#rulesSearch");
        input.ShouldNotBeNull();
        input.GetAttribute("placeholder").ShouldBe("Search name or pattern...");
    }

    /// <summary>
    /// Verifies that the category dropdown is rendered with options.
    /// </summary>
    [Fact]
    public void RendersCategoryDropdown()
    {
        var categories = new List<BudgetCategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Groceries", Type = "Expense", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Utilities", Type = "Expense", IsActive = true },
        };

        var cut = RenderToolbar(categories: categories);

        var select = cut.Find("#rulesCategoryFilter");
        select.ShouldNotBeNull();
        var options = cut.FindAll("#rulesCategoryFilter option");
        options.Count.ShouldBe(3); // All Categories + 2
        options[0].TextContent.ShouldBe("All Categories");
        options[1].TextContent.ShouldBe("Groceries");
        options[2].TextContent.ShouldBe("Utilities");
    }

    /// <summary>
    /// Verifies that the status dropdown is rendered with all options.
    /// </summary>
    [Fact]
    public void RendersStatusDropdown()
    {
        var cut = RenderToolbar();

        var options = cut.FindAll("#rulesStatusFilter option");
        options.Count.ShouldBe(3);
        options[0].TextContent.ShouldBe("All");
        options[1].TextContent.ShouldBe("Active");
        options[2].TextContent.ShouldBe("Inactive");
    }

    /// <summary>
    /// Verifies that search input fires OnSearchChanged callback.
    /// </summary>
    [Fact]
    public void SearchInput_FiresCallback()
    {
        string? captured = null;
        var cut = RenderToolbar(onSearchChanged: value =>
        {
            captured = value;
            return Task.CompletedTask;
        });

        cut.Find("#rulesSearch").Input("grocery");

        captured.ShouldBe("grocery");
    }

    /// <summary>
    /// Verifies that empty/whitespace search passes null to callback.
    /// </summary>
    [Fact]
    public void SearchInput_PassesNullForWhitespace()
    {
        string? captured = "initial";
        var cut = RenderToolbar(onSearchChanged: value =>
        {
            captured = value;
            return Task.CompletedTask;
        });

        cut.Find("#rulesSearch").Input("   ");

        captured.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that category dropdown fires OnCategoryChanged with parsed Guid.
    /// </summary>
    [Fact]
    public void CategoryDropdown_FiresCallback()
    {
        var categoryId = Guid.NewGuid();
        var categories = new List<BudgetCategoryDto>
        {
            new() { Id = categoryId, Name = "Groceries", Type = "Expense", IsActive = true },
        };

        Guid? captured = null;
        var cut = RenderToolbar(
            categories: categories,
            onCategoryChanged: value =>
            {
                captured = value;
                return Task.CompletedTask;
            });

        cut.Find("#rulesCategoryFilter").Change(categoryId.ToString());

        captured.ShouldBe(categoryId);
    }

    /// <summary>
    /// Verifies that selecting "All Categories" passes null to callback.
    /// </summary>
    [Fact]
    public void CategoryDropdown_PassesNull_WhenAllSelected()
    {
        Guid? captured = Guid.NewGuid();
        var cut = RenderToolbar(onCategoryChanged: value =>
        {
            captured = value;
            return Task.CompletedTask;
        });

        cut.Find("#rulesCategoryFilter").Change(string.Empty);

        captured.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that status dropdown fires OnStatusChanged callback.
    /// </summary>
    [Fact]
    public void StatusDropdown_FiresCallback()
    {
        string? captured = null;
        var cut = RenderToolbar(onStatusChanged: value =>
        {
            captured = value;
            return Task.CompletedTask;
        });

        cut.Find("#rulesStatusFilter").Change("Active");

        captured.ShouldBe("Active");
    }

    /// <summary>
    /// Verifies that selecting "All" status passes null to callback.
    /// </summary>
    [Fact]
    public void StatusDropdown_PassesNull_WhenAllSelected()
    {
        string? captured = "Active";
        var cut = RenderToolbar(onStatusChanged: value =>
        {
            captured = value;
            return Task.CompletedTask;
        });

        cut.Find("#rulesStatusFilter").Change(string.Empty);

        captured.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that badge is not rendered when no active filters.
    /// </summary>
    [Fact]
    public void Badge_NotRendered_WhenNoActiveFilters()
    {
        var cut = RenderToolbar(activeFilterCount: 0);

        cut.FindAll(".badge").Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that badge is rendered with correct count when filters are active.
    /// </summary>
    [Fact]
    public void Badge_RendersCount_WhenFiltersActive()
    {
        var cut = RenderToolbar(activeFilterCount: 2);

        cut.Markup.ShouldContain("2");
    }

    /// <summary>
    /// Verifies that clear filters button fires callback.
    /// </summary>
    [Fact]
    public void ClearFilters_FiresCallback()
    {
        var called = false;
        var cut = RenderToolbar(
            activeFilterCount: 1,
            onClearFilters: () =>
            {
                called = true;
                return Task.CompletedTask;
            });

        var buttons = cut.FindAll("button");
        var clearButton = buttons.First(b => b.TextContent.Contains("Clear Filters"));
        clearButton.Click();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that clear filters button is disabled when no filters active.
    /// </summary>
    [Fact]
    public void ClearFilters_DisabledWhenNoFilters()
    {
        var cut = RenderToolbar(activeFilterCount: 0);

        var buttons = cut.FindAll("button");
        var clearButton = buttons.First(b => b.TextContent.Contains("Clear Filters"));
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that the component renders the filter-section wrapper.
    /// </summary>
    [Fact]
    public void RendersFilterSection()
    {
        var cut = RenderToolbar();

        cut.FindAll(".filter-section").Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that the group by category button is rendered.
    /// </summary>
    [Fact]
    public void RendersGroupByCategoryButton()
    {
        var cut = RenderToolbar();

        var buttons = cut.FindAll("button");
        buttons.ShouldContain(b => b.TextContent.Contains("Group by Category"));
    }

    /// <summary>
    /// Verifies that clicking group toggle fires OnGroupToggle callback.
    /// </summary>
    [Fact]
    public void GroupToggle_FiresCallback()
    {
        var called = false;
        var cut = RenderToolbar(onGroupToggle: () =>
        {
            called = true;
            return Task.CompletedTask;
        });

        var buttons = cut.FindAll("button");
        var groupButton = buttons.First(b => b.TextContent.Contains("Group by Category"));
        groupButton.Click();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that the view toggle buttons are rendered.
    /// </summary>
    [Fact]
    public void RendersViewToggleButtons()
    {
        var cut = RenderToolbar();

        var toggle = cut.Find(".view-toggle");
        toggle.ShouldNotBeNull();
        var buttons = toggle.QuerySelectorAll("button");
        buttons.Length.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that clicking Card view button fires OnViewModeChanged callback.
    /// </summary>
    [Fact]
    public void ViewToggle_ClickCard_FiresCallback()
    {
        RulesViewMode? receivedMode = null;
        var cut = RenderToolbar(onViewModeChanged: mode =>
        {
            receivedMode = mode;
            return Task.CompletedTask;
        });

        var toggle = cut.Find(".view-toggle");
        var buttons = toggle.QuerySelectorAll("button");
        buttons[1].Click(); // Card button is second

        receivedMode.ShouldBe(RulesViewMode.Card);
    }

    /// <summary>
    /// Verifies that the active Table button has primary style when in Table mode.
    /// </summary>
    [Fact]
    public void ViewToggle_TableActive_HasPrimaryClass()
    {
        var cut = RenderToolbar(viewMode: RulesViewMode.Table);

        var toggle = cut.Find(".view-toggle");
        var buttons = toggle.QuerySelectorAll("button");
        buttons[0].ClassName!.ShouldContain("btn-primary");
        buttons[1].ClassName!.ShouldNotContain("btn-primary");
    }

    private IRenderedComponent<RulesToolbar> RenderToolbar(
        string? searchText = null,
        Guid? categoryId = null,
        string? status = null,
        IReadOnlyList<BudgetCategoryDto>? categories = null,
        int activeFilterCount = 0,
        bool isGrouped = false,
        RulesViewMode viewMode = RulesViewMode.Table,
        Func<string?, Task>? onSearchChanged = null,
        Func<Guid?, Task>? onCategoryChanged = null,
        Func<string?, Task>? onStatusChanged = null,
        Func<Task>? onClearFilters = null,
        Func<RulesViewMode, Task>? onViewModeChanged = null,
        Func<Task>? onGroupToggle = null)
    {
        return Render<RulesToolbar>(parameters => parameters
            .Add(p => p.SearchText, searchText)
            .Add(p => p.CategoryId, categoryId)
            .Add(p => p.Status, status)
            .Add(p => p.Categories, categories ?? [])
            .Add(p => p.ActiveFilterCount, activeFilterCount)
            .Add(p => p.IsGrouped, isGrouped)
            .Add(p => p.ViewMode, viewMode)
            .Add(p => p.OnSearchChanged, onSearchChanged ?? (_ => Task.CompletedTask))
            .Add(p => p.OnCategoryChanged, onCategoryChanged ?? (_ => Task.CompletedTask))
            .Add(p => p.OnStatusChanged, onStatusChanged ?? (_ => Task.CompletedTask))
            .Add(p => p.OnClearFilters, onClearFilters ?? (() => Task.CompletedTask))
            .Add(p => p.OnViewModeChanged, onViewModeChanged ?? (_ => Task.CompletedTask))
            .Add(p => p.OnGroupToggle, onGroupToggle ?? (() => Task.CompletedTask)));
    }
}

// <copyright file="RulesTableTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="RulesTable"/> component.
/// </summary>
public sealed class RulesTableTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RulesTableTests"/> class.
    /// </summary>
    public RulesTableTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <summary>
    /// Verifies empty state message when no rules are provided.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoRules()
    {
        var cut = RenderTable([]);

        cut.Markup.ShouldContain("No rules match the current filters.");
        cut.FindAll("table").Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies table renders with column headers.
    /// </summary>
    [Fact]
    public void RendersTableHeaders()
    {
        var cut = RenderTable([CreateRule("Test", "pat", "Contains")]);

        cut.Markup.ShouldContain("Priority");
        cut.Markup.ShouldContain("Name");
        cut.Markup.ShouldContain("Pattern");
        cut.Markup.ShouldContain("Match Type");
        cut.Markup.ShouldContain("Category");
        cut.Markup.ShouldContain("Status");
        cut.Markup.ShouldContain("Actions");
    }

    /// <summary>
    /// Verifies rule name, pattern, category, and priority are rendered.
    /// </summary>
    [Fact]
    public void RendersRuleDetails()
    {
        var rule = CreateRule("Grocery Matcher", "GROCERY", "Contains", priority: 5);
        var cut = RenderTable([rule]);

        cut.Markup.ShouldContain("Grocery Matcher");
        cut.Markup.ShouldContain("GROCERY");
        cut.Markup.ShouldContain("#5");
        cut.Markup.ShouldContain("Test Category");
    }

    /// <summary>
    /// Verifies active rules show Active badge.
    /// </summary>
    [Fact]
    public void ShowsActiveBadge_ForActiveRule()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains", isActive: true)]);

        cut.Markup.ShouldContain("Active");
    }

    /// <summary>
    /// Verifies inactive rules show Inactive badge.
    /// </summary>
    [Fact]
    public void ShowsInactiveBadge_ForInactiveRule()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains", isActive: false)]);

        cut.Markup.ShouldContain("Inactive");
    }

    /// <summary>
    /// Verifies match type badge is rendered.
    /// </summary>
    [Fact]
    public void RendersMatchTypeBadge()
    {
        var cut = RenderTable([CreateRule("R", "P", "Exact")]);

        cut.Markup.ShouldContain("Exact");
        cut.Markup.ShouldContain("badge-primary");
    }

    /// <summary>
    /// Verifies Contains match type gets badge-info class.
    /// </summary>
    [Fact]
    public void ContainsMatchType_UsesBadgeInfo()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains")]);

        cut.Markup.ShouldContain("badge-info");
    }

    /// <summary>
    /// Verifies case sensitive indicator is shown.
    /// </summary>
    [Fact]
    public void ShowsCaseSensitiveIndicator()
    {
        var rule = CreateRule("R", "P", "Contains");
        rule.CaseSensitive = true;

        var cut = RenderTable([rule]);

        cut.Markup.ShouldContain("Aa");
    }

    /// <summary>
    /// Verifies case sensitive indicator is hidden when not case sensitive.
    /// </summary>
    [Fact]
    public void HidesCaseSensitiveIndicator_WhenNotCaseSensitive()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains")]);

        cut.Markup.ShouldNotContain(">Aa<");
    }

    /// <summary>
    /// Verifies multiple rules render as rows.
    /// </summary>
    [Fact]
    public void RendersMultipleRules()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains", priority: 1),
            CreateRule("Rule B", "B", "Exact", priority: 2),
            CreateRule("Rule C", "C", "Regex", priority: 3),
        };

        var cut = RenderTable(rules);

        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies edit button invokes OnEdit callback.
    /// </summary>
    [Fact]
    public void EditButton_InvokesCallback()
    {
        var rule = CreateRule("Test", "PAT", "Contains");
        CategorizationRuleDto? edited = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.OnEdit, (CategorizationRuleDto r) => { edited = r; }));

        var editBtn = cut.FindAll("button[title='Edit rule']");
        editBtn.Count.ShouldBe(1);
        editBtn[0].Click();

        edited.ShouldNotBeNull();
        edited.Id.ShouldBe(rule.Id);
    }

    /// <summary>
    /// Verifies delete button invokes OnDelete callback.
    /// </summary>
    [Fact]
    public void DeleteButton_InvokesCallback()
    {
        var rule = CreateRule("Test", "PAT", "Contains");
        CategorizationRuleDto? deleted = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.OnDelete, (CategorizationRuleDto r) => { deleted = r; }));

        var deleteBtn = cut.FindAll("button[title='Delete rule']");
        deleteBtn.Count.ShouldBe(1);
        deleteBtn[0].Click();

        deleted.ShouldNotBeNull();
        deleted.Id.ShouldBe(rule.Id);
    }

    /// <summary>
    /// Verifies deactivate button shown for active rule, invokes OnDeactivate callback.
    /// </summary>
    [Fact]
    public void DeactivateButton_ShownForActiveRule_InvokesCallback()
    {
        var rule = CreateRule("Test", "PAT", "Contains", isActive: true);
        CategorizationRuleDto? deactivated = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.OnDeactivate, (CategorizationRuleDto r) => { deactivated = r; }));

        var deactivateBtn = cut.FindAll("button[title='Deactivate rule']");
        deactivateBtn.Count.ShouldBe(1);
        deactivateBtn[0].Click();

        deactivated.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies activate button shown for inactive rule, invokes OnActivate callback.
    /// </summary>
    [Fact]
    public void ActivateButton_ShownForInactiveRule_InvokesCallback()
    {
        var rule = CreateRule("Test", "PAT", "Contains", isActive: false);
        CategorizationRuleDto? activated = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.OnActivate, (CategorizationRuleDto r) => { activated = r; }));

        var activateBtn = cut.FindAll("button[title='Activate rule']");
        activateBtn.Count.ShouldBe(1);
        activateBtn[0].Click();

        activated.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies inactive rule row has the row-inactive CSS class.
    /// </summary>
    [Fact]
    public void InactiveRule_HasRowInactiveClass()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains", isActive: false)]);

        var row = cut.Find("tbody tr");
        row.ClassList.ShouldContain("row-inactive");
    }

    /// <summary>
    /// Verifies active rule row does not have row-inactive CSS class.
    /// </summary>
    [Fact]
    public void ActiveRule_DoesNotHaveRowInactiveClass()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains", isActive: true)]);

        var row = cut.Find("tbody tr");
        row.ClassList.ShouldNotContain("row-inactive");
    }

    /// <summary>
    /// Verifies Unknown is shown when CategoryName is null.
    /// </summary>
    [Fact]
    public void ShowsUnknown_WhenCategoryNameNull()
    {
        var rule = CreateRule("R", "P", "Contains");
        rule.CategoryName = null;

        var cut = RenderTable([rule]);

        cut.Markup.ShouldContain("Unknown");
    }

    // --- Sort Headers ---

    /// <summary>
    /// Verifies that clicking a sortable header invokes OnSort with the correct field.
    /// </summary>
    [Fact]
    public void SortableHeader_InvokesOnSort_WithCorrectField()
    {
        string? sortedField = null;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.OnSort, (string field) => { sortedField = field; }));

        var nameHeader = cut.FindAll("th.sortable-header")[1]; // second sortable header = name
        nameHeader.Click();

        sortedField.ShouldBe("name");
    }

    /// <summary>
    /// Verifies that the priority header triggers sort with "priority" field.
    /// </summary>
    [Fact]
    public void PriorityHeader_InvokesOnSort_WithPriority()
    {
        string? sortedField = null;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.OnSort, (string field) => { sortedField = field; }));

        var priorityHeader = cut.FindAll("th.sortable-header")[0];
        priorityHeader.Click();

        sortedField.ShouldBe("priority");
    }

    /// <summary>
    /// Verifies that the category header triggers sort with "category" field.
    /// </summary>
    [Fact]
    public void CategoryHeader_InvokesOnSort_WithCategory()
    {
        string? sortedField = null;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.OnSort, (string field) => { sortedField = field; }));

        var categoryHeader = cut.FindAll("th.sortable-header")[2];
        categoryHeader.Click();

        sortedField.ShouldBe("category");
    }

    /// <summary>
    /// Verifies that active sort column shows ascending indicator.
    /// </summary>
    [Fact]
    public void ActiveSortColumn_ShowsAscendingIndicator()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.SortBy, "name")
            .Add(p => p.SortDirection, "asc"));

        var nameHeader = cut.FindAll("th.sortable-header")[1];
        nameHeader.InnerHtml.ShouldContain("sort-active");
        nameHeader.InnerHtml.ShouldContain("\u25B2"); // ▲
    }

    /// <summary>
    /// Verifies that active sort column shows descending indicator.
    /// </summary>
    [Fact]
    public void ActiveSortColumn_ShowsDescendingIndicator()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.SortBy, "name")
            .Add(p => p.SortDirection, "desc"));

        var nameHeader = cut.FindAll("th.sortable-header")[1];
        nameHeader.InnerHtml.ShouldContain("sort-active");
        nameHeader.InnerHtml.ShouldContain("\u25BC"); // ▼
    }

    /// <summary>
    /// Verifies that inactive sort columns show the inactive indicator.
    /// </summary>
    [Fact]
    public void InactiveSortColumn_ShowsInactiveIndicator()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.SortBy, "name")
            .Add(p => p.SortDirection, "asc"));

        var priorityHeader = cut.FindAll("th.sortable-header")[0]; // priority is not the active sort
        priorityHeader.InnerHtml.ShouldContain("sort-inactive");
    }

    /// <summary>
    /// Verifies that the active sort column has the correct aria-sort attribute.
    /// </summary>
    [Fact]
    public void ActiveSortColumn_HasCorrectAriaSort()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.SortBy, "priority")
            .Add(p => p.SortDirection, "desc"));

        var priorityHeader = cut.FindAll("th.sortable-header")[0];
        priorityHeader.GetAttribute("aria-sort").ShouldBe("descending");
    }

    /// <summary>
    /// Verifies that inactive sort columns have aria-sort="none".
    /// </summary>
    [Fact]
    public void InactiveSortColumn_HasAriaSortNone()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.SortBy, "priority")
            .Add(p => p.SortDirection, "asc"));

        var nameHeader = cut.FindAll("th.sortable-header")[1];
        nameHeader.GetAttribute("aria-sort").ShouldBe("none");
    }

    // --- Selection Checkbox Tests ---

    /// <summary>
    /// Verifies header checkbox renders in the table.
    /// </summary>
    [Fact]
    public void RendersHeaderCheckbox()
    {
        var cut = RenderTable([CreateRule("R", "P", "Contains")]);

        var headerCheckbox = cut.Find("thead input[type='checkbox']");
        headerCheckbox.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies row checkboxes render for each rule.
    /// </summary>
    [Fact]
    public void RendersRowCheckboxes()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("R1", "P1", "Contains"),
            CreateRule("R2", "P2", "Contains"),
        };
        var cut = RenderTable(rules);

        var rowCheckboxes = cut.FindAll("tbody input[type='checkbox']");
        rowCheckboxes.Count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies row checkbox invokes OnSelectionToggled with the rule ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RowCheckbox_InvokesOnSelectionToggled()
    {
        var rule = CreateRule("R1", "P1", "Contains");
        Guid? toggledId = null;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.OnSelectionToggled, id =>
            {
                toggledId = id;
                return Task.CompletedTask;
            }));

        var checkbox = cut.Find("tbody input[type='checkbox']");
        await checkbox.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

        toggledId.ShouldBe(rule.Id);
    }

    /// <summary>
    /// Verifies header checkbox invokes OnSelectAllToggle.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HeaderCheckbox_InvokesOnSelectAllToggle()
    {
        var selectAllCalled = false;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.OnSelectAllToggle, () =>
            {
                selectAllCalled = true;
                return Task.CompletedTask;
            }));

        var headerCheckbox = cut.Find("thead input[type='checkbox']");
        await headerCheckbox.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true });

        selectAllCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies selected row checkbox has checked attribute.
    /// </summary>
    [Fact]
    public void SelectedRule_CheckboxIsChecked()
    {
        var rule = CreateRule("R1", "P1", "Contains");
        var selectedIds = new HashSet<Guid> { rule.Id };
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.SelectedIds, selectedIds));

        var checkbox = cut.Find("tbody input[type='checkbox']");
        checkbox.HasAttribute("checked").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies non-selected row checkbox is not checked.
    /// </summary>
    [Fact]
    public void UnselectedRule_CheckboxIsNotChecked()
    {
        var rule = CreateRule("R1", "P1", "Contains");
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.SelectedIds, new HashSet<Guid>()));

        var checkbox = cut.Find("tbody input[type='checkbox']");
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies header checkbox is checked when AreAllSelected is true.
    /// </summary>
    [Fact]
    public void HeaderCheckbox_IsChecked_WhenAllSelected()
    {
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.AreAllSelected, true));

        var headerCheckbox = cut.Find("thead input[type='checkbox']");
        headerCheckbox.HasAttribute("checked").ShouldBeTrue();
    }

    // --- Keyboard Navigation Tests ---

    /// <summary>
    /// Verifies that ArrowDown moves focus to the first row when no row is focused.
    /// </summary>
    [Fact]
    public void ArrowDown_FocusesFirstRow_WhenNoRowFocused()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
            CreateRule("Rule B", "B", "Contains"),
        };
        var cut = RenderTable(rules);

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown");

        var firstRow = cut.Find("#rules-row-0");
        firstRow.ClassList.ShouldContain("row-focused");
    }

    /// <summary>
    /// Verifies that ArrowDown moves focus from the first to the second row.
    /// </summary>
    [Fact]
    public void ArrowDown_MovesFocusToNextRow()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
            CreateRule("Rule B", "B", "Contains"),
        };
        var cut = RenderTable(rules);

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0
        table.KeyDown(key: "ArrowDown"); // focus row 1

        var secondRow = cut.Find("#rules-row-1");
        secondRow.ClassList.ShouldContain("row-focused");
        secondRow.GetAttribute("tabindex").ShouldBe("0");
    }

    /// <summary>
    /// Verifies that ArrowDown does not go past the last row.
    /// </summary>
    [Fact]
    public void ArrowDown_StopsAtLastRow()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
        };
        var cut = RenderTable(rules);

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0
        table.KeyDown(key: "ArrowDown"); // try beyond

        var row = cut.Find("#rules-row-0");
        row.ClassList.ShouldContain("row-focused");
    }

    /// <summary>
    /// Verifies that ArrowUp moves focus to the previous row.
    /// </summary>
    [Fact]
    public void ArrowUp_MovesFocusToPreviousRow()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
            CreateRule("Rule B", "B", "Contains"),
        };
        var cut = RenderTable(rules);

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0
        table.KeyDown(key: "ArrowDown"); // focus row 1
        table.KeyDown(key: "ArrowUp");   // back to row 0

        var firstRow = cut.Find("#rules-row-0");
        firstRow.ClassList.ShouldContain("row-focused");
        firstRow.GetAttribute("tabindex").ShouldBe("0");
    }

    /// <summary>
    /// Verifies that Enter on a focused row invokes OnEdit.
    /// </summary>
    [Fact]
    public void Enter_InvokesOnEdit_ForFocusedRow()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
            CreateRule("Rule B", "B", "Contains"),
        };
        CategorizationRuleDto? edited = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnEdit, (CategorizationRuleDto r) => { edited = r; }));

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0
        table.KeyDown(key: "Enter");

        edited.ShouldNotBeNull();
        edited.Id.ShouldBe(rules[0].Id);
    }

    /// <summary>
    /// Verifies that Space on a focused row toggles the selection.
    /// </summary>
    [Fact]
    public void Space_TogglesSelection_ForFocusedRow()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
        };
        Guid? toggledId = null;

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnSelectionToggled, (Guid id) => { toggledId = id; }));

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0
        table.KeyDown(key: " ");

        toggledId.ShouldBe(rules[0].Id);
    }

    /// <summary>
    /// Verifies that Escape invokes OnClearSelection.
    /// </summary>
    [Fact]
    public void Escape_InvokesOnClearSelection()
    {
        var clearCalled = false;
        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { CreateRule("R", "P", "Contains") })
            .Add(p => p.OnClearSelection, () =>
            {
                clearCalled = true;
                return Task.CompletedTask;
            }));

        var table = cut.Find("table");
        table.KeyDown(key: "Escape");

        clearCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that focused row has tabindex 0 and non-focused rows have tabindex -1.
    /// </summary>
    [Fact]
    public void FocusedRow_HasTabIndexZero_OtherRowsHaveNegativeOne()
    {
        var rules = new List<CategorizationRuleDto>
        {
            CreateRule("Rule A", "A", "Contains"),
            CreateRule("Rule B", "B", "Contains"),
        };
        var cut = RenderTable(rules);

        var table = cut.Find("table");
        table.KeyDown(key: "ArrowDown"); // focus row 0

        var row0 = cut.Find("#rules-row-0");
        var row1 = cut.Find("#rules-row-1");
        row0.GetAttribute("tabindex").ShouldBe("0");
        row1.GetAttribute("tabindex").ShouldBe("-1");
    }

    /// <summary>
    /// Verifies that rows have aria-selected based on selection state.
    /// </summary>
    [Fact]
    public void SelectedRow_HasAriaSelectedTrue()
    {
        var rule = CreateRule("Rule A", "A", "Contains");
        var selectedIds = new HashSet<Guid> { rule.Id };

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.SelectedIds, selectedIds));

        var row = cut.Find("tbody tr");
        row.GetAttribute("aria-selected").ShouldBe("true");
    }

    /// <summary>
    /// Verifies that unselected rows have aria-selected=false.
    /// </summary>
    [Fact]
    public void UnselectedRow_HasAriaSelectedFalse()
    {
        var rule = CreateRule("Rule A", "A", "Contains");

        var cut = Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, new List<CategorizationRuleDto> { rule })
            .Add(p => p.SelectedIds, new HashSet<Guid>()));

        var row = cut.Find("tbody tr");
        row.GetAttribute("aria-selected").ShouldBe("false");
    }

    private static CategorizationRuleDto CreateRule(
        string name,
        string pattern,
        string matchType,
        int priority = 1,
        bool isActive = true)
    {
        return new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Pattern = pattern,
            MatchType = matchType,
            CaseSensitive = false,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Test Category",
            Priority = priority,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private IRenderedComponent<RulesTable> RenderTable(IReadOnlyList<CategorizationRuleDto> rules)
    {
        return Render<RulesTable>(parameters => parameters
            .Add(p => p.Rules, rules));
    }
}

// <copyright file="InlineCategoryPickerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Transactions;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Transactions;

/// <summary>
/// Unit tests for the <see cref="InlineCategoryPicker"/> component.
/// </summary>
public sealed class InlineCategoryPickerTests : BunitContext
{
    private readonly List<BudgetCategoryDto> _categories;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineCategoryPickerTests"/> class.
    /// </summary>
    public InlineCategoryPickerTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this._categories = new List<BudgetCategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Groceries", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Utilities", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Archived", IsActive = false },
        };
    }

    /// <summary>
    /// Verifies the component displays the category name when not editing.
    /// </summary>
    [Fact]
    public void Render_ShowsCategoryName_WhenCategoryIsSet()
    {
        var cut = RenderPicker(categoryId: this._categories[0].Id, categoryName: "Groceries");

        cut.Find("span").TextContent.Trim().ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies the component displays "Uncategorized" when no category is set.
    /// </summary>
    [Fact]
    public void Render_ShowsUncategorized_WhenNoCategorySet()
    {
        var cut = RenderPicker(categoryId: null, categoryName: null);

        cut.Find("span").TextContent.Trim().ShouldBe("Uncategorized");
        cut.Find("span").ClassList.ShouldContain("text-muted");
    }

    /// <summary>
    /// Verifies the component shows a clickable span by default (not editing mode).
    /// </summary>
    [Fact]
    public void Render_ShowsClickableSpan_ByDefault()
    {
        var cut = RenderPicker();

        cut.Find("span.category-clickable").ShouldNotBeNull();
        cut.FindAll("select").Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies clicking the label switches to edit mode with a select dropdown.
    /// </summary>
    [Fact]
    public void Click_SwitchesToEditMode_ShowsDropdown()
    {
        var cut = RenderPicker();

        cut.Find("span").Click();

        cut.FindAll("select").Count.ShouldBe(1);
        cut.FindAll("span.category-clickable").Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies the dropdown shows only active categories plus Uncategorized.
    /// </summary>
    [Fact]
    public void EditMode_ShowsOnlyActiveCategories()
    {
        var cut = RenderPicker();
        cut.Find("span").Click();

        var options = cut.Find("select").QuerySelectorAll("option");

        // "Uncategorized" + 2 active categories (not the archived one)
        options.Length.ShouldBe(3);
    }

    /// <summary>
    /// Verifies selecting a category invokes the callback with the new category ID.
    /// </summary>
    [Fact]
    public void SelectCategory_InvokesOnCategoryChanged()
    {
        Guid? receivedCategoryId = null;
        var cut = RenderPicker(
            categoryId: null,
            categoryName: null,
            onCategoryChanged: id => receivedCategoryId = id);

        cut.Find("span").Click();
        cut.Find("select").Change(this._categories[0].Id.ToString());

        receivedCategoryId.ShouldBe(this._categories[0].Id);
    }

    /// <summary>
    /// Verifies selecting "Uncategorized" invokes the callback with null.
    /// </summary>
    [Fact]
    public void SelectUncategorized_InvokesOnCategoryChanged_WithNull()
    {
        Guid? receivedCategoryId = Guid.NewGuid(); // Start with non-null to verify it changes
        var cut = RenderPicker(
            categoryId: this._categories[0].Id,
            categoryName: "Groceries",
            onCategoryChanged: id => receivedCategoryId = id);

        cut.Find("span").Click();
        cut.Find("select").Change(string.Empty);

        receivedCategoryId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies selecting the same category does not invoke the callback.
    /// </summary>
    [Fact]
    public void SelectSameCategory_DoesNotInvokeCallback()
    {
        var callbackInvoked = false;
        var existingCategoryId = this._categories[0].Id;
        var cut = RenderPicker(
            categoryId: existingCategoryId,
            categoryName: "Groceries",
            onCategoryChanged: _ => callbackInvoked = true);

        cut.Find("span").Click();
        cut.Find("select").Change(existingCategoryId.ToString());

        callbackInvoked.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that after selecting a category, the dropdown closes (returns to label mode).
    /// </summary>
    [Fact]
    public void SelectCategory_ClosesDropdown()
    {
        var cut = RenderPicker(onCategoryChanged: _ => { });

        cut.Find("span").Click();
        cut.Find("select").Change(this._categories[0].Id.ToString());

        cut.FindAll("select").Count.ShouldBe(0);
        cut.FindAll("span").Count.ShouldBeGreaterThan(0);
    }

    private IRenderedComponent<InlineCategoryPicker> RenderPicker(
        Guid? categoryId = null,
        string? categoryName = null,
        Action<Guid?>? onCategoryChanged = null)
    {
        return Render<InlineCategoryPicker>(parameters => parameters
            .Add(p => p.CategoryId, categoryId)
            .Add(p => p.CategoryName, categoryName)
            .Add(p => p.Categories, this._categories)
            .Add(p => p.OnCategoryChanged, onCategoryChanged ?? (_ => { })));
    }
}

// <copyright file="CategoryCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="CategoryCard"/> component.
/// </summary>
public class CategoryCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryCardTests"/> class.
    /// </summary>
    public CategoryCardTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies category name is displayed.
    /// </summary>
    [Fact]
    public void DisplaysCategoryName()
    {
        var cut = RenderCard(name: "Groceries");

        Assert.Contains("Groceries", cut.Markup);
    }

    /// <summary>
    /// Verifies category type is displayed.
    /// </summary>
    [Fact]
    public void DisplaysCategoryType()
    {
        var cut = RenderCard(type: "Expense");

        Assert.Contains("Expense", cut.Markup);
    }

    /// <summary>
    /// Verifies active category does not show inactive badge.
    /// </summary>
    [Fact]
    public void ActiveCategory_NoInactiveBadge()
    {
        var cut = RenderCard(isActive: true);

        Assert.DoesNotContain("Inactive", cut.Markup);
    }

    /// <summary>
    /// Verifies inactive category shows inactive badge.
    /// </summary>
    [Fact]
    public void InactiveCategory_ShowsInactiveBadge()
    {
        var cut = RenderCard(isActive: false);

        Assert.Contains("Inactive", cut.Markup);
    }

    /// <summary>
    /// Verifies inactive category has inactive CSS class.
    /// </summary>
    [Fact]
    public void InactiveCategory_HasInactiveCssClass()
    {
        var cut = RenderCard(isActive: false);

        Assert.Contains("card-inactive", cut.Markup);
    }

    /// <summary>
    /// Verifies active category shows deactivate button.
    /// </summary>
    [Fact]
    public void ActiveCategory_ShowsDeactivateButton()
    {
        var cut = RenderCard(isActive: true);

        var deactivateBtn = cut.Find("button[title='Deactivate category']");
        Assert.NotNull(deactivateBtn);
    }

    /// <summary>
    /// Verifies inactive category shows activate button.
    /// </summary>
    [Fact]
    public void InactiveCategory_ShowsActivateButton()
    {
        var cut = RenderCard(isActive: false);

        var activateBtn = cut.Find("button[title='Activate category']");
        Assert.NotNull(activateBtn);
    }

    /// <summary>
    /// Verifies edit button fires OnEdit callback.
    /// </summary>
    [Fact]
    public void EditButton_FiresOnEditCallback()
    {
        BudgetCategoryDto? edited = null;
        var category = CreateCategory(name: "Food");

        var cut = Render<CategoryCard>(p => p
            .Add(x => x.Category, category)
            .Add(x => x.OnEdit, (BudgetCategoryDto c) =>
            {
                edited = c;
                return Task.CompletedTask;
            }));

        var editBtn = cut.Find("button[title='Edit category']");
        editBtn.Click();

        Assert.NotNull(edited);
        Assert.Equal("Food", edited!.Name);
    }

    /// <summary>
    /// Verifies delete button fires OnDelete callback.
    /// </summary>
    [Fact]
    public void DeleteButton_FiresOnDeleteCallback()
    {
        BudgetCategoryDto? deleted = null;
        var category = CreateCategory(name: "Transport");

        var cut = Render<CategoryCard>(p => p
            .Add(x => x.Category, category)
            .Add(x => x.OnDelete, (BudgetCategoryDto c) =>
            {
                deleted = c;
                return Task.CompletedTask;
            }));

        var deleteBtn = cut.Find("button[title='Delete category']");
        deleteBtn.Click();

        Assert.NotNull(deleted);
        Assert.Equal("Transport", deleted!.Name);
    }

    /// <summary>
    /// Verifies deactivate button fires OnDeactivate callback.
    /// </summary>
    [Fact]
    public void DeactivateButton_FiresCallback()
    {
        BudgetCategoryDto? deactivated = null;
        var category = CreateCategory(isActive: true);

        var cut = Render<CategoryCard>(p => p
            .Add(x => x.Category, category)
            .Add(x => x.OnDeactivate, (BudgetCategoryDto c) =>
            {
                deactivated = c;
                return Task.CompletedTask;
            }));

        var deactivateBtn = cut.Find("button[title='Deactivate category']");
        deactivateBtn.Click();

        Assert.NotNull(deactivated);
    }

    /// <summary>
    /// Verifies activate button fires OnActivate callback.
    /// </summary>
    [Fact]
    public void ActivateButton_FiresCallback()
    {
        BudgetCategoryDto? activated = null;
        var category = CreateCategory(isActive: false);

        var cut = Render<CategoryCard>(p => p
            .Add(x => x.Category, category)
            .Add(x => x.OnActivate, (BudgetCategoryDto c) =>
            {
                activated = c;
                return Task.CompletedTask;
            }));

        var activateBtn = cut.Find("button[title='Activate category']");
        activateBtn.Click();

        Assert.NotNull(activated);
    }

    /// <summary>
    /// Verifies icon emoji renders correctly.
    /// </summary>
    /// <param name="icon">The icon name to test.</param>
    /// <param name="expectedEmoji">The expected emoji output.</param>
    [Theory]
    [InlineData("cart", "🛒")]
    [InlineData("utensils", "🍽️")]
    [InlineData("book", "📚")]
    [InlineData(null, "📁")]
    public void DisplaysCorrectIconEmoji(string? icon, string expectedEmoji)
    {
        var cut = RenderCard(icon: icon);

        Assert.Contains(expectedEmoji, cut.Markup);
    }

    private static BudgetCategoryDto CreateCategory(
        string name = "Test Category",
        string type = "Expense",
        bool isActive = true,
        string? icon = null,
        string? color = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = type,
        IsActive = isActive,
        Icon = icon,
        Color = color,
    };

    private IRenderedComponent<CategoryCard> RenderCard(
        string name = "Test Category",
        string type = "Expense",
        bool isActive = true,
        string? icon = null,
        string? color = null)
    {
        var category = CreateCategory(name, type, isActive, icon, color);
        return Render<CategoryCard>(p => p
            .Add(x => x.Category, category));
    }
}

// <copyright file="CategoriesPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Categories"/> page component.
/// </summary>
public class CategoriesPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesPageTests"/> class.
    /// </summary>
    public CategoriesPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<IChatContextService>(new StubChatContextService());
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Categories>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Budget Categories");
    }

    /// <summary>
    /// Verifies empty message is shown when no categories exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyMessage_WhenNoCategories()
    {
        var cut = Render<Categories>();

        cut.Markup.ShouldContain("No categories yet");
    }

    /// <summary>
    /// Verifies expense category section is rendered.
    /// </summary>
    [Fact]
    public void ShowsExpenseSection_WhenExpenseCategoriesExist()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));

        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Expense Categories");
    }

    /// <summary>
    /// Verifies income category section is rendered.
    /// </summary>
    [Fact]
    public void ShowsIncomeSection_WhenIncomeCategoriesExist()
    {
        this._apiService.Categories.Add(CreateCategory("Salary", "Income"));

        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Income Categories");
    }

    /// <summary>
    /// Verifies transfer category section is rendered.
    /// </summary>
    [Fact]
    public void ShowsTransferSection_WhenTransferCategoriesExist()
    {
        this._apiService.Categories.Add(CreateCategory("Account Transfer", "Transfer"));

        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Transfer Categories");
    }

    /// <summary>
    /// Verifies categories are grouped by type.
    /// </summary>
    [Fact]
    public void GroupsCategoriesByType()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        this._apiService.Categories.Add(CreateCategory("Salary", "Income"));
        this._apiService.Categories.Add(CreateCategory("Transfer Out", "Transfer"));

        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Expense Categories");
        cut.Markup.ShouldContain("Income Categories");
        cut.Markup.ShouldContain("Transfer Categories");
    }

    /// <summary>
    /// Verifies the category count badge is shown per section.
    /// </summary>
    [Fact]
    public void ShowsCategoryCountBadge()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        this._apiService.Categories.Add(CreateCategory("Dining", "Expense"));

        var cut = Render<Categories>();

        var badges = cut.FindAll(".badge-secondary");
        badges.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Verifies the Add Category button is present.
    /// </summary>
    [Fact]
    public void HasAddCategoryButton()
    {
        var cut = Render<Categories>();

        cut.Markup.ShouldContain("Add Category");
    }

    /// <summary>
    /// Verifies no expense section when only income categories exist.
    /// </summary>
    [Fact]
    public void HidesExpenseSection_WhenNoExpenseCategories()
    {
        this._apiService.Categories.Add(CreateCategory("Salary", "Income"));

        var cut = Render<Categories>();

        cut.Markup.ShouldNotContain("Expense Categories");
    }

    /// <summary>
    /// Verifies no income section when only expense categories exist.
    /// </summary>
    [Fact]
    public void HidesIncomeSection_WhenNoIncomeCategories()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));

        var cut = Render<Categories>();

        cut.Markup.ShouldNotContain("Income Categories");
    }

    /// <summary>
    /// Verifies the delete confirm dialog is hidden by default.
    /// </summary>
    [Fact]
    public void DeleteConfirmDialog_IsHiddenByDefault()
    {
        var cut = Render<Categories>();

        // The ConfirmDialog should not be visible ('Are you sure' text hidden or dialog not visible)
        cut.Markup.ShouldNotContain("Are you sure you want to delete");
    }

    private static BudgetCategoryDto CreateCategory(string name, string type, bool isActive = true)
    {
        return new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Icon = "tag",
            Color = "#4CAF50",
            IsActive = isActive,
            SortOrder = 0,
        };
    }
}

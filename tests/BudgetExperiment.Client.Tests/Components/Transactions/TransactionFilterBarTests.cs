// <copyright file="TransactionFilterBarTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="TransactionFilterBar"/> component.
/// </summary>
public sealed class TransactionFilterBarTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionFilterBarTests"/> class.
    /// </summary>
    public TransactionFilterBarTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// Verifies the filter bar renders with default state.
    /// </summary>
    [Fact]
    public void Render_ShowsFilterSection()
    {
        var cut = RenderFilterBar();

        cut.Find(".filter-section").ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies account dropdown renders accounts.
    /// </summary>
    [Fact]
    public void Render_ShowsAccountDropdown_WithAccounts()
    {
        var accounts = new List<AccountDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Checking" },
            new() { Id = Guid.NewGuid(), Name = "Savings" },
        };

        var cut = RenderFilterBar(accounts: accounts);

        var accountSelect = cut.Find("#txnAccountFilter");
        var options = accountSelect.QuerySelectorAll("option");
        options.Length.ShouldBe(3); // "All Accounts" + 2 accounts
    }

    /// <summary>
    /// Verifies category dropdown renders categories with Uncategorized option.
    /// </summary>
    [Fact]
    public void Render_ShowsCategoryDropdown_WithUncategorizedOption()
    {
        var categories = new List<BudgetCategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Groceries" },
        };

        var cut = RenderFilterBar(categories: categories);

        var categorySelect = cut.Find("#txnCategoryFilter");
        var options = categorySelect.QuerySelectorAll("option");
        options.Length.ShouldBe(3); // "All Categories" + "Uncategorized" + 1 category
    }

    /// <summary>
    /// Verifies changing account filter invokes OnFiltersApplied.
    /// </summary>
    [Fact]
    public void AccountChange_InvokesOnFiltersApplied()
    {
        var filtersApplied = false;
        var filter = new UnifiedTransactionFilterDto();
        var accountId = Guid.NewGuid();
        var accounts = new List<AccountDto> { new() { Id = accountId, Name = "Checking" } };

        var cut = RenderFilterBar(filter: filter, accounts: accounts, onApplied: () => filtersApplied = true);

        cut.Find("#txnAccountFilter").Change(accountId.ToString());

        filtersApplied.ShouldBeTrue();
        filter.AccountId.ShouldBe(accountId);
    }

    /// <summary>
    /// Verifies selecting "Uncategorized" sets the Uncategorized filter.
    /// </summary>
    [Fact]
    public void CategoryChange_ToUncategorized_SetsFilter()
    {
        var filter = new UnifiedTransactionFilterDto();
        var filtersApplied = false;

        var cut = RenderFilterBar(filter: filter, onApplied: () => filtersApplied = true);

        cut.Find("#txnCategoryFilter").Change("uncategorized");

        filtersApplied.ShouldBeTrue();
        filter.Uncategorized.ShouldBe(true);
        filter.CategoryId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies selecting a specific category sets CategoryId and clears Uncategorized.
    /// </summary>
    [Fact]
    public void CategoryChange_ToSpecificCategory_SetsCategoryId()
    {
        var filter = new UnifiedTransactionFilterDto { Uncategorized = true };
        var categoryId = Guid.NewGuid();
        var categories = new List<BudgetCategoryDto> { new() { Id = categoryId, Name = "Groceries" } };

        var cut = RenderFilterBar(filter: filter, categories: categories, onApplied: () => { });

        cut.Find("#txnCategoryFilter").Change(categoryId.ToString());

        filter.CategoryId.ShouldBe(categoryId);
        filter.Uncategorized.ShouldBeNull();
    }

    /// <summary>
    /// Verifies clearing filters invokes OnFiltersCleared.
    /// </summary>
    [Fact]
    public void ClearButton_InvokesOnFiltersCleared()
    {
        var filtersCleared = false;

        var cut = RenderFilterBar(onCleared: () => filtersCleared = true);

        // The Clear Filters button is the last button in the filter section
        var buttons = cut.FindAll("button");
        var clearButton = buttons.First(b => b.TextContent.Contains("Clear Filters"));
        clearButton.Click();

        filtersCleared.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies active filter count badge is shown when filters are active.
    /// </summary>
    [Fact]
    public void Render_ShowsBadge_WhenActiveFiltersExist()
    {
        var cut = RenderFilterBar(activeFilterCount: 3);

        var badge = cut.Find(".badge");
        badge.TextContent.ShouldContain("3");
    }

    /// <summary>
    /// Verifies no badge is shown when no filters are active.
    /// </summary>
    [Fact]
    public void Render_NoBadge_WhenNoActiveFilters()
    {
        var cut = RenderFilterBar(activeFilterCount: 0);

        cut.FindAll(".badge").Count.ShouldBe(0);
    }

    private IRenderedComponent<TransactionFilterBar> RenderFilterBar(
        UnifiedTransactionFilterDto? filter = null,
        IReadOnlyList<AccountDto>? accounts = null,
        IReadOnlyList<BudgetCategoryDto>? categories = null,
        int activeFilterCount = 0,
        Action? onApplied = null,
        Action? onCleared = null)
    {
        return Render<TransactionFilterBar>(parameters => parameters
            .Add(p => p.Filter, filter ?? new UnifiedTransactionFilterDto())
            .Add(p => p.Accounts, accounts ?? [])
            .Add(p => p.Categories, categories ?? [])
            .Add(p => p.ActiveFilterCount, activeFilterCount)
            .Add(p => p.OnFiltersApplied, onApplied ?? (() => { }))
            .Add(p => p.OnFiltersCleared, onCleared ?? (() => { })));
    }
}

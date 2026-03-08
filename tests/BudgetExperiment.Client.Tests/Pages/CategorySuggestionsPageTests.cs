// <copyright file="CategorySuggestionsPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="CategorySuggestions"/> page component.
/// </summary>
public class CategorySuggestionsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubCategorySuggestionApiService _suggestionService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionsPageTests"/> class.
    /// </summary>
    public CategorySuggestionsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ICategorySuggestionApiService>(this._suggestionService);
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
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is correct.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Category Suggestions");
    }

    /// <summary>
    /// Verifies the subtitle is rendered.
    /// </summary>
    [Fact]
    public void HasSubtitle()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("AI-powered suggestions");
    }

    /// <summary>
    /// Verifies empty state is shown when no pending suggestions exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoPendingSuggestions()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("No Category Suggestions");
    }

    /// <summary>
    /// Verifies the Analyze Transactions button is present.
    /// </summary>
    [Fact]
    public void HasAnalyzeButton()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Analyze Transactions");
    }

    /// <summary>
    /// Verifies the Refresh button is present.
    /// </summary>
    [Fact]
    public void HasRefreshButton()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Refresh");
    }

    /// <summary>
    /// Verifies tab buttons are present.
    /// </summary>
    [Fact]
    public void HasTabButtons()
    {
        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Pending");
        cut.Markup.ShouldContain("Dismissed");
    }

    /// <summary>
    /// Verifies the Pending tab is active by default.
    /// </summary>
    [Fact]
    public void PendingTab_IsActiveByDefault()
    {
        var cut = Render<CategorySuggestions>();

        var activeTab = cut.Find(".tab-button.active");
        activeTab.TextContent.Trim().ShouldContain("Pending");
    }

    /// <summary>
    /// Verifies suggestion cards are rendered when pending suggestions exist.
    /// </summary>
    [Fact]
    public void ShowsSuggestionCards_WhenPendingSuggestionsExist()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Groceries"));

        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies switching to Dismissed tab shows empty state.
    /// </summary>
    [Fact]
    public void DismissedTab_ShowsEmptyState_WhenNoDismissedSuggestions()
    {
        var cut = Render<CategorySuggestions>();

        var dismissedTab = cut.FindAll(".tab-button")[1];
        dismissedTab.Click();

        cut.Markup.ShouldContain("No Dismissed Suggestions");
    }

    /// <summary>
    /// Verifies Accept Selected button appears when suggestions are selected.
    /// </summary>
    [Fact]
    public void AcceptSelectedButton_NotVisible_WhenNothingSelected()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Test Category"));

        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldNotContain("Accept Selected");
    }

    /// <summary>
    /// Verifies multiple suggestions render as a list.
    /// </summary>
    [Fact]
    public void ShowsMultipleSuggestions()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Groceries"));
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Entertainment"));
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Transportation"));

        var cut = Render<CategorySuggestions>();

        cut.Markup.ShouldContain("Groceries");
        cut.Markup.ShouldContain("Entertainment");
        cut.Markup.ShouldContain("Transportation");
    }

    private static CategorySuggestionDto CreateSuggestion(string name)
    {
        return new CategorySuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedName = name,
            SuggestedType = "Expense",
            Confidence = 0.85m,
            MerchantPatterns = new[] { name.ToUpperInvariant() },
            MatchingTransactionCount = 5,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

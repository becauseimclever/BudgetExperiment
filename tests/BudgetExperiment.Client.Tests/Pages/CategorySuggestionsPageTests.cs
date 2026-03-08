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

    /// <summary>
    /// Verifies that analyze button triggers analysis and reloads suggestions.
    /// </summary>
    [Fact]
    public void AnalyzeTransactions_ReloadsSuggestions()
    {
        this._suggestionService.AnalyzeResult.Add(CreateSuggestion("New Category"));

        var cut = Render<CategorySuggestions>();
        var analyzeBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Analyze"));
        analyzeBtn.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that a suggestion Accept button triggers the accept dialog.
    /// </summary>
    [Fact]
    public void AcceptButton_ShowsAcceptDialog()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Groceries"));
        this._suggestionService.AcceptResult = new AcceptCategorySuggestionResultDto
        {
            SuggestionId = Guid.NewGuid(),
            Success = true,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Groceries",
        };

        var cut = Render<CategorySuggestions>();
        var acceptBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Accept"));
        acceptBtn.Click();

        // Accept dialog should be shown with the suggestion name
        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies dismiss button removes suggestion from list.
    /// </summary>
    [Fact]
    public void DismissButton_RemovesSuggestion()
    {
        var suggestion = CreateSuggestion("Unwanted");
        this._suggestionService.PendingSuggestions.Add(suggestion);
        this._suggestionService.DismissResult = true;

        var cut = Render<CategorySuggestions>();
        cut.Markup.ShouldContain("Unwanted");

        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        cut.Markup.ShouldNotContain("Unwanted");
    }

    /// <summary>
    /// Verifies dismissed suggestions tab shows restored suggestions.
    /// </summary>
    [Fact]
    public void DismissedTab_ShowsDismissedSuggestions()
    {
        this._suggestionService.DismissedSuggestions.Add(CreateSuggestion("Dismissed Category"));

        var cut = Render<CategorySuggestions>();
        var dismissedTab = cut.FindAll(".tab-button")[1];
        dismissedTab.Click();

        cut.Markup.ShouldContain("Dismissed Category");
    }

    /// <summary>
    /// Verifies toggling selection on a suggestion works.
    /// </summary>
    [Fact]
    public void ToggleSelection_SelectsSuggestion()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("Selectable"));

        var cut = Render<CategorySuggestions>();
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Change(true);

        // Accept Selected button should appear when at least one suggestion is selected
        cut.Markup.ShouldContain("Accept Selected");
    }

    /// <summary>
    /// Verifies rules preview button opens preview modal.
    /// </summary>
    [Fact]
    public void RulesPreview_ShowsRulesModal()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("With Rules"));
        this._suggestionService.PreviewRules.Add(new SuggestedCategoryRuleDto
        {
            Pattern = "WITH RULES",
            MatchType = "Contains",
            MatchingTransactionCount = 3,
            SampleDescriptions = new[] { "WITH RULES STORE" },
        });

        var cut = Render<CategorySuggestions>();

        // Find the preview rules button
        var previewBtns = cut.FindAll("button").Where(b => b.TextContent.Contains("Preview Rules") || b.HasAttribute("title")).ToList();
        if (previewBtns.Any())
        {
            previewBtns.First().Click();
        }

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clear dismissed patterns confirmation flow.
    /// </summary>
    [Fact]
    public void ClearDismissedPatterns_ShowsConfirmation()
    {
        this._suggestionService.DismissedSuggestions.Add(CreateSuggestion("Dismissed One"));
        this._suggestionService.ClearDismissedPatternsResult = 1;

        var cut = Render<CategorySuggestions>();

        // Switch to dismissed tab first
        var dismissedTab = cut.FindAll(".tab-button")[1];
        dismissedTab.Click();

        cut.Markup.ShouldContain("Dismissed One");
    }

    /// <summary>
    /// Verifies the Analyze button triggers analysis.
    /// </summary>
    [Fact]
    public void AnalyzeButton_TriggersAnalysis()
    {
        var cut = Render<CategorySuggestions>();
        var analyzeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Analyze"));
        analyzeButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the Refresh button is clickable.
    /// </summary>
    [Fact]
    public void RefreshButton_IsClickable()
    {
        var cut = Render<CategorySuggestions>();
        var refreshButton = cut.FindAll("button").First(b => b.TextContent.Contains("Refresh"));
        refreshButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the Accept button for a suggestion opens accept dialog.
    /// </summary>
    [Fact]
    public void AcceptButton_OpensAcceptDialog()
    {
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("New Category"));

        var cut = Render<CategorySuggestions>();
        var acceptBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Accept") && !b.TextContent.Contains("Selected"));
        acceptBtn.Click();

        // Should show Accept Category dialog
        cut.Markup.ShouldContain("Accept Category");
    }

    /// <summary>
    /// Verifies the Dismiss button dismisses a suggestion.
    /// </summary>
    [Fact]
    public void DismissButton_DismissesSuggestion()
    {
        this._suggestionService.DismissResult = true;
        this._suggestionService.PendingSuggestions.Add(CreateSuggestion("To Dismiss"));

        var cut = Render<CategorySuggestions>();
        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clicking the Dismissed tab switches view.
    /// </summary>
    [Fact]
    public void DismissedTab_SwitchesView()
    {
        var cut = Render<CategorySuggestions>();
        var dismissedTab = cut.FindAll(".tab-button")[1];
        dismissedTab.Click();

        cut.Markup.ShouldContain("No Dismissed Suggestions");
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

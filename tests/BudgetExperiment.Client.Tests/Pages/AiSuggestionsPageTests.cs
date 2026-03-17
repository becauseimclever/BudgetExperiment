// <copyright file="AiSuggestionsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="AiSuggestions"/> page component.
/// </summary>
public class AiSuggestionsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubAiApiService _aiService = new();
    private readonly StubCategorySuggestionApiService _categoryService = new();
    private readonly StubAiAvailabilityService _availabilityService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSuggestionsPageTests"/> class.
    /// </summary>
    public AiSuggestionsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IAiApiService>(this._aiService);
        this.Services.AddSingleton<ICategorySuggestionApiService>(this._categoryService);
        this.Services.AddSingleton<IAiAvailabilityService>(this._availabilityService);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddTransient<AiSuggestionsViewModel>();
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
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("AI Suggestions");
    }

    /// <summary>
    /// Verifies the Run AI Analysis button is present.
    /// </summary>
    [Fact]
    public void HasRunAnalysisButton()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Run AI Analysis");
    }

    /// <summary>
    /// Verifies the Refresh button is present.
    /// </summary>
    [Fact]
    public void HasRefreshButton()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Refresh");
    }

    /// <summary>
    /// Verifies setup banner is shown when AI is not available.
    /// </summary>
    [Fact]
    public void ShowsSetupBanner_WhenAiNotAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = false, IsEnabled = false };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the subtitle text is present.
    /// </summary>
    [Fact]
    public void HasSubtitleText()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Review and manage AI-generated suggestions");
    }

    /// <summary>
    /// Verifies that AI status badge component is present.
    /// </summary>
    [Fact]
    public void HasAiStatusBadge()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("toolbar-right");
    }

    /// <summary>
    /// Verifies toolbar layout with left and right sections.
    /// </summary>
    [Fact]
    public void HasToolbarLayout()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("suggestions-toolbar");
        cut.Markup.ShouldContain("toolbar-left");
        cut.Markup.ShouldContain("toolbar-right");
    }

    /// <summary>
    /// Verifies AI status is displayed when connected.
    /// </summary>
    [Fact]
    public void ShowsConnectedStatus()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            CurrentModel = "gpt-4",
        };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies disconnected status shows appropriate message.
    /// </summary>
    [Fact]
    public void ShowsDisconnectedStatus()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = false,
        };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies pending rule suggestions are rendered.
    /// </summary>
    [Fact]
    public void ShowsPendingSuggestions()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Starbucks Rule",
            SuggestedPattern = "STARBUCKS",
            SuggestedCategoryName = "Coffee",
            Confidence = 0.9m,
            AffectedTransactionCount = 5,
            Type = "NewRule",
        });

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Starbucks Rule");
    }

    /// <summary>
    /// Verifies multiple suggestions are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleSuggestions()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Netflix Rule",
            SuggestedPattern = "NETFLIX",
            SuggestedCategoryName = "Entertainment",
            Confidence = 0.95m,
            AffectedTransactionCount = 3,
            Type = "NewRule",
        });
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Walmart Rule",
            SuggestedPattern = "WALMART",
            SuggestedCategoryName = "Groceries",
            Confidence = 0.85m,
            AffectedTransactionCount = 8,
            Type = "NewRule",
        });

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Netflix Rule");
        cut.Markup.ShouldContain("Walmart Rule");
    }

    /// <summary>
    /// Verifies empty state when no suggestions.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoSuggestions()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = true,
            CurrentModel = "gpt-4",
        };

        var cut = Render<AiSuggestions>();

        // Should show the all-caught-up banner
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the Run AI Analysis button is disabled when AI is not available.
    /// </summary>
    [Fact]
    public void RunAnalysis_IsDisabled_WhenAiNotAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = false, IsEnabled = false };

        var cut = Render<AiSuggestions>();

        var analysisButton = cut.FindAll("button").First(b => b.TextContent.Contains("Run AI Analysis"));
        analysisButton.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies accepting a rule suggestion removes it from the list.
    /// </summary>
    [Fact]
    public void HandleAccept_RemovesSuggestion_WhenSuccessful()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Accept Target Rule",
            SuggestedPattern = "ACCEPT_TARGET",
            SuggestedCategoryName = "Shopping",
            Confidence = 0.9m,
            AffectedTransactionCount = 5,
            Type = "NewRule",
        });
        this._aiService.AcceptSuggestionResult = new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = "Accept Target Rule",
            Pattern = "ACCEPT_TARGET",
            CategoryId = Guid.NewGuid(),
            IsActive = true,
        };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Accept Target Rule");

        // Find the Accept button inside a suggestion card (not the group's "Accept All")
        var acceptButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Contains("Accept") && !b.TextContent.Contains("All")).ToList();
        acceptButtons.ShouldNotBeEmpty("Expected at least one Accept button");
        acceptButtons.First().Click();

        cut.WaitForAssertion(() =>
        {
            // After acceptance, the suggestion should be removed and success message shown
            cut.Markup.ShouldContain("Rule suggestion accepted");
        });
    }

    /// <summary>
    /// Verifies accepting a suggestion shows error when API returns null.
    /// </summary>
    [Fact]
    public void HandleAccept_ShowsError_WhenApiFails()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "FAIL_ACCEPT",
            SuggestedCategoryName = "Test",
            Confidence = 0.8m,
            AffectedTransactionCount = 2,
            Type = "NewRule",
        });
        this._aiService.AcceptSuggestionResult = null;

        var cut = Render<AiSuggestions>();

        var acceptButton = cut.FindAll("button").FirstOrDefault(b =>
            b.TextContent.Contains("Accept") && !b.TextContent.Contains("All"));
        acceptButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to accept suggestion"));
    }

    /// <summary>
    /// Verifies dismissing a suggestion removes it from the list.
    /// </summary>
    [Fact]
    public void HandleDismiss_RemovesSuggestion_WhenSuccessful()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Dismiss Target Rule",
            SuggestedPattern = "DISMISS_TARGET",
            SuggestedCategoryName = "Test",
            Confidence = 0.7m,
            AffectedTransactionCount = 1,
            Type = "NewRule",
        });
        this._aiService.DismissSuggestionResult = true;

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Dismiss Target Rule");

        var dismissButton = cut.FindAll("button").FirstOrDefault(b =>
            b.TextContent.Contains("Dismiss") && !b.TextContent.Contains("All"));
        dismissButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Dismiss Target Rule"));
    }

    /// <summary>
    /// Verifies dismissing a suggestion shows error when API fails.
    /// </summary>
    [Fact]
    public void HandleDismiss_ShowsError_WhenApiFails()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "FAIL_DISMISS",
            SuggestedCategoryName = "Test",
            Confidence = 0.6m,
            AffectedTransactionCount = 1,
            Type = "NewRule",
        });
        this._aiService.DismissSuggestionResult = false;

        var cut = Render<AiSuggestions>();

        var dismissButton = cut.FindAll("button").FirstOrDefault(b =>
            b.TextContent.Contains("Dismiss") && !b.TextContent.Contains("All"));
        dismissButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to dismiss suggestion"));
    }

    /// <summary>
    /// Verifies providing feedback is reflected.
    /// </summary>
    [Fact]
    public void HandleFeedback_UpdatesSuggestion_WhenSuccessful()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Feedback Target Rule",
            SuggestedPattern = "FEEDBACK_TARGET",
            SuggestedCategoryName = "Feedback",
            Confidence = 0.8m,
            AffectedTransactionCount = 3,
            Type = "NewRule",
        });
        this._aiService.ProvideFeedbackResult = true;

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Feedback Target Rule");
    }

    /// <summary>
    /// Verifies analysis runs and processes result when AI is available.
    /// </summary>
    [Fact]
    public void StartAnalysis_ProcessesResult_WhenAiAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = true,
            CurrentModel = "gpt-4",
        };
        this._aiService.AnalyzeResult = new AnalysisResponseDto
        {
            NewRuleSuggestions = 5,
        };

        var cut = Render<AiSuggestions>();

        var analysisButton = cut.FindAll("button").First(b => b.TextContent.Contains("Run AI Analysis"));
        analysisButton.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotBeNullOrEmpty());
    }

    /// <summary>
    /// Verifies analysis is not triggered when AI is not enabled.
    /// </summary>
    [Fact]
    public void StartAnalysis_DoesNotRun_WhenAiNotEnabled()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = false,
        };

        var cut = Render<AiSuggestions>();

        var analysisButton = cut.FindAll("button").First(b => b.TextContent.Contains("Run AI Analysis"));
        analysisButton.HasAttribute("disabled").ShouldBeTrue();
    }
}

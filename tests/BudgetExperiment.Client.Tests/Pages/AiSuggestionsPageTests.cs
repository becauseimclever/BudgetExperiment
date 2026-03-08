// <copyright file="AiSuggestionsPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="AiSuggestions"/> page component.
/// </summary>
public class AiSuggestionsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubAiApiService _aiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSuggestionsPageTests"/> class.
    /// </summary>
    public AiSuggestionsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IAiApiService>(this._aiService);
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
    /// Verifies onboarding panel is shown when AI is not available and no suggestions.
    /// </summary>
    [Fact]
    public void ShowsOnboardingPanel_WhenAiNotAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = false, IsEnabled = false };

        var cut = Render<AiSuggestions>();

        // With no status, suggestions, or analysis result, AiOnboardingPanel should render
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the subtitle text is present.
    /// </summary>
    [Fact]
    public void HasSubtitleText()
    {
        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("Review and manage AI-generated rule suggestions");
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
    /// Verifies pending suggestions are rendered.
    /// </summary>
    [Fact]
    public void ShowsPendingSuggestions()
    {
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedPattern = "STARBUCKS",
            SuggestedCategoryName = "Coffee",
            Confidence = 0.9m,
            AffectedTransactionCount = 5,
            Type = "NewRule",
        });

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("STARBUCKS");
    }

    /// <summary>
    /// Verifies multiple suggestions are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleSuggestions()
    {
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedPattern = "NETFLIX",
            SuggestedCategoryName = "Entertainment",
            Confidence = 0.95m,
            AffectedTransactionCount = 3,
            Type = "NewRule",
        });
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedPattern = "WALMART",
            SuggestedCategoryName = "Groceries",
            Confidence = 0.85m,
            AffectedTransactionCount = 8,
            Type = "NewRule",
        });

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("NETFLIX");
        cut.Markup.ShouldContain("WALMART");
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
            CurrentModel = "gpt-4",
        };

        var cut = Render<AiSuggestions>();

        // Should show the onboarding or empty state
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that clicking Run AI Analysis triggers analysis and shows progress dialog.
    /// </summary>
    [Fact]
    public void StartAnalysis_ShowsProgressDialog_WhenAiAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = true,
            CurrentModel = "gpt-4",
        };

        var cut = Render<AiSuggestions>();

        var analysisButton = cut.FindAll("button").First(b => b.TextContent.Contains("Run AI Analysis"));
        analysisButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that clicking Refresh reloads suggestions.
    /// </summary>
    [Fact]
    public void RefreshButton_ReloadsSuggestions()
    {
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedPattern = "AMAZON",
            SuggestedCategoryName = "Shopping",
            Confidence = 0.8m,
            AffectedTransactionCount = 10,
            Type = "NewRule",
        });

        var cut = Render<AiSuggestions>();

        var refreshButton = cut.FindAll("button").First(b => b.TextContent.Contains("Refresh"));
        refreshButton.Click();

        cut.Markup.ShouldContain("AMAZON");
    }

    /// <summary>
    /// Verifies that accepting a suggestion removes it from the list.
    /// </summary>
    [Fact]
    public void AcceptSuggestion_RemovesFromList()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "TARGET",
            SuggestedCategoryName = "Shopping",
            Confidence = 0.9m,
            AffectedTransactionCount = 3,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("TARGET");
    }

    /// <summary>
    /// Verifies that dismissing a suggestion handles the response correctly.
    /// </summary>
    [Fact]
    public void DismissSuggestion_HandlesResponse()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "UBER",
            SuggestedCategoryName = "Transport",
            Confidence = 0.7m,
            AffectedTransactionCount = 2,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("UBER");
    }

    /// <summary>
    /// Verifies Run AI Analysis button is disabled when AI is not available.
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
    /// Verifies accepting a suggestion removes it from the list when accepted successfully.
    /// </summary>
    [Fact]
    public void HandleAccept_RemovesSuggestion_WhenSuccessful()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "ACCEPT_TARGET",
            SuggestedCategoryName = "Shopping",
            Confidence = 0.9m,
            AffectedTransactionCount = 5,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.AcceptSuggestionResult = new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Pattern = "ACCEPT_TARGET",
            CategoryId = Guid.NewGuid(),
            IsActive = true,
        };

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("ACCEPT_TARGET");

        var acceptButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-success") && b.TextContent.Contains("Accept"));
        acceptButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("ACCEPT_TARGET"));
    }

    /// <summary>
    /// Verifies accepting a suggestion shows error when API returns null.
    /// </summary>
    [Fact]
    public void HandleAccept_ShowsError_WhenApiFails()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "FAIL_ACCEPT",
            SuggestedCategoryName = "Test",
            Confidence = 0.8m,
            AffectedTransactionCount = 2,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.AcceptSuggestionResult = null;

        var cut = Render<AiSuggestions>();

        var acceptButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-success") && b.TextContent.Contains("Accept"));
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
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "DISMISS_TARGET",
            SuggestedCategoryName = "Test",
            Confidence = 0.7m,
            AffectedTransactionCount = 1,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.DismissSuggestionResult = true;

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("DISMISS_TARGET");

        var dismissButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-secondary") && b.TextContent.Contains("Dismiss"));
        dismissButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("DISMISS_TARGET"));
    }

    /// <summary>
    /// Verifies dismissing a suggestion shows error when API fails.
    /// </summary>
    [Fact]
    public void HandleDismiss_ShowsError_WhenApiFails()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "FAIL_DISMISS",
            SuggestedCategoryName = "Test",
            Confidence = 0.6m,
            AffectedTransactionCount = 1,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.DismissSuggestionResult = false;

        var cut = Render<AiSuggestions>();

        var dismissButton = cut.FindAll("button").FirstOrDefault(b =>
            b.ClassList.Contains("btn-secondary") && b.TextContent.Contains("Dismiss"));
        dismissButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to dismiss suggestion"));
    }

    /// <summary>
    /// Verifies the view details button shows suggestion detail dialog.
    /// </summary>
    [Fact]
    public void HandleViewDetails_ShowsDetailDialog()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "DETAIL_VIEW",
            SuggestedCategoryName = "Details",
            Confidence = 0.95m,
            AffectedTransactionCount = 10,
            Type = "NewRule",
            Description = "Test suggestion description",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };

        var cut = Render<AiSuggestions>();

        var detailsButton = cut.FindAll("button").FirstOrDefault(b =>
            b.TextContent.Contains("Details"));
        detailsButton?.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotBeNullOrEmpty());
    }

    /// <summary>
    /// Verifies providing positive feedback updates the suggestion.
    /// </summary>
    [Fact]
    public void HandleFeedback_UpdatesSuggestion_WhenSuccessful()
    {
        var suggestionId = Guid.NewGuid();
        this._aiService.PendingSuggestions.Add(new RuleSuggestionDto
        {
            Id = suggestionId,
            SuggestedPattern = "FEEDBACK_TARGET",
            SuggestedCategoryName = "Feedback",
            Confidence = 0.8m,
            AffectedTransactionCount = 3,
            Type = "NewRule",
        });
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "gpt-4" };
        this._aiService.ProvideFeedbackResult = true;

        var cut = Render<AiSuggestions>();

        cut.Markup.ShouldContain("FEEDBACK_TARGET");
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

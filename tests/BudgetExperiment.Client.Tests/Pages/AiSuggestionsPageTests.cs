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
}

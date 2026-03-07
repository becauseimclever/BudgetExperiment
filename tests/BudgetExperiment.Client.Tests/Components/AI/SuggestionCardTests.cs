// <copyright file="SuggestionCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Unit tests for the <see cref="SuggestionCard"/> component.
/// </summary>
public sealed class SuggestionCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionCardTests"/> class.
    /// </summary>
    public SuggestionCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the card shows the suggestion title.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsTitle()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(title: "Test Title")));

        Assert.Contains("Test Title", cut.Markup);
    }

    /// <summary>
    /// Verifies the card shows the description.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsDescription()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Found 15 transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies the confidence percentage is displayed.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsConfidencePercentage()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(confidence: 0.85m)));

        Assert.Contains("85%", cut.Markup);
    }

    /// <summary>
    /// Verifies the type badge shows correct label for NewRule.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsNewRuleTypeBadge()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(type: "NewRule")));

        Assert.Contains("New Rule", cut.Markup);
        Assert.Contains("type-new-rule", cut.Markup);
    }

    /// <summary>
    /// Verifies the type badge shows correct label for PatternOptimization.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsOptimizationTypeBadge()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(type: "PatternOptimization")));

        Assert.Contains("Optimization", cut.Markup);
        Assert.Contains("type-optimization", cut.Markup);
    }

    /// <summary>
    /// Verifies the suggested pattern is displayed.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsSuggestedPattern()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("STARBUCKS", cut.Markup);
        Assert.Contains("Contains", cut.Markup);
    }

    /// <summary>
    /// Verifies the category name is displayed.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsCategoryName()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Coffee", cut.Markup);
    }

    /// <summary>
    /// Verifies the impact count is displayed.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsImpactCount()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("15 transaction(s) affected", cut.Markup);
    }

    /// <summary>
    /// Verifies accept button invokes callback with suggestion ID.
    /// </summary>
    [Fact]
    public void SuggestionCard_AcceptButton_InvokesCallback()
    {
        Guid? acceptedId = null;
        var suggestion = CreateSuggestion();

        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, suggestion)
            .Add(p => p.OnAccept, (Guid id) => { acceptedId = id; }));

        var acceptBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Accept"));
        acceptBtn.Click();

        Assert.Equal(suggestion.Id, acceptedId);
    }

    /// <summary>
    /// Verifies dismiss button invokes callback.
    /// </summary>
    [Fact]
    public void SuggestionCard_DismissButton_InvokesCallback()
    {
        Guid? dismissedId = null;
        var suggestion = CreateSuggestion();

        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, suggestion)
            .Add(p => p.OnDismiss, (Guid id) => { dismissedId = id; }));

        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        Assert.Equal(suggestion.Id, dismissedId);
    }

    /// <summary>
    /// Verifies sample descriptions are shown.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsSampleDescriptions()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Sample matches", cut.Markup);
        Assert.Contains("STARBUCKS #123", cut.Markup);
        Assert.Contains("STARBUCKS DOWNTOWN", cut.Markup);
    }

    /// <summary>
    /// Verifies feedback buttons are present.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsFeedbackButtons()
    {
        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Helpful?", cut.Markup);
        var feedbackBtns = cut.FindAll(".feedback-btn");
        Assert.Equal(2, feedbackBtns.Count);
    }

    /// <summary>
    /// Verifies details button invokes callback.
    /// </summary>
    [Fact]
    public void SuggestionCard_DetailsButton_InvokesCallback()
    {
        Guid? viewedId = null;
        var suggestion = CreateSuggestion();

        var cut = Render<SuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, suggestion)
            .Add(p => p.OnViewDetails, (Guid id) => { viewedId = id; }));

        var detailsBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Details"));
        detailsBtn.Click();

        Assert.Equal(suggestion.Id, viewedId);
    }

    private static RuleSuggestionDto CreateSuggestion(
        string type = "NewRule",
        string title = "Categorize coffee purchases",
        decimal confidence = 0.9m) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Status = "Pending",
        Title = title,
        Description = "Found 15 transactions matching coffee shop patterns.",
        Confidence = confidence,
        SuggestedPattern = "STARBUCKS",
        SuggestedMatchType = "Contains",
        SuggestedCategoryName = "Coffee",
        AffectedTransactionCount = 15,
        SampleDescriptions = ["STARBUCKS #123", "STARBUCKS DOWNTOWN", "STARBUCKS MAIN ST"],
        CreatedAtUtc = DateTime.UtcNow,
    };
}

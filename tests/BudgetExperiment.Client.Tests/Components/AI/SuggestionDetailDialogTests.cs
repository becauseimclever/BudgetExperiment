// <copyright file="SuggestionDetailDialogTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="SuggestionDetailDialog"/> component.
/// </summary>
public sealed class SuggestionDetailDialogTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionDetailDialogTests"/> class.
    /// </summary>
    public SuggestionDetailDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the dialog shows suggestion title.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsTitle()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Categorize grocery purchases", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows description.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsDescription()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Found patterns for grocery store transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows AI reasoning.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsReasoning()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("Multiple transactions share similar patterns", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows confidence bar.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsConfidence()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("92%", cut.Markup);
        Assert.Contains("confidence-bar", cut.Markup);
    }

    /// <summary>
    /// Verifies rule preview section shows pattern and category.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsRulePreview()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("KROGER", cut.Markup);
        Assert.Contains("Contains", cut.Markup);
        Assert.Contains("Groceries", cut.Markup);
    }

    /// <summary>
    /// Verifies sample descriptions are shown.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsSamples()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("KROGER #456", cut.Markup);
        Assert.Contains("KROGER STORE 789", cut.Markup);
    }

    /// <summary>
    /// Verifies the accept button invokes callback.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_AcceptButton_InvokesCallback()
    {
        Guid? acceptedId = null;
        var suggestion = CreateSuggestion();

        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
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
    public void SuggestionDetailDialog_DismissButton_InvokesCallback()
    {
        Guid? dismissedId = null;
        var suggestion = CreateSuggestion();

        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, suggestion)
            .Add(p => p.OnDismiss, (Guid id) => { dismissedId = id; }));

        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        Assert.Equal(suggestion.Id, dismissedId);
    }

    /// <summary>
    /// Verifies feedback buttons are present.
    /// </summary>
    [Fact]
    public void SuggestionDetailDialog_ShowsFeedbackButtons()
    {
        var cut = Render<SuggestionDetailDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Suggestion, CreateSuggestion()));

        var feedbackBtns = cut.FindAll(".feedback-btn");
        Assert.Equal(2, feedbackBtns.Count);
    }

    private static RuleSuggestionDto CreateSuggestion() => new()
    {
        Id = Guid.NewGuid(),
        Type = "NewRule",
        Status = "Pending",
        Title = "Categorize grocery purchases",
        Description = "Found patterns for grocery store transactions.",
        Reasoning = "Multiple transactions share similar patterns.",
        Confidence = 0.92m,
        SuggestedPattern = "KROGER",
        SuggestedMatchType = "Contains",
        SuggestedCategoryName = "Groceries",
        AffectedTransactionCount = 20,
        SampleDescriptions = ["KROGER #456", "KROGER STORE 789"],
        CreatedAtUtc = DateTime.UtcNow,
    };
}

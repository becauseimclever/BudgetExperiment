// <copyright file="AnalysisSummaryCardTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="AnalysisSummaryCard"/> component.
/// </summary>
public sealed class AnalysisSummaryCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisSummaryCardTests"/> class.
    /// </summary>
    public AnalysisSummaryCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the card shows analysis results title.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ShowsTitle()
    {
        var result = new AnalysisResponseDto();

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("Analysis Results", cut.Markup);
    }

    /// <summary>
    /// Verifies stat badges show suggestion counts.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ShowsStatBadges()
    {
        var result = new AnalysisResponseDto
        {
            NewRuleSuggestions = 5,
            OptimizationSuggestions = 3,
            ConflictSuggestions = 1,
        };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("5", cut.Markup);
        Assert.Contains("3", cut.Markup);
        Assert.Contains("1", cut.Markup);
    }

    /// <summary>
    /// Verifies the card starts expanded showing details.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_StartsExpanded()
    {
        var result = new AnalysisResponseDto
        {
            UncategorizedTransactionsAnalyzed = 25,
            RulesAnalyzed = 8,
        };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("expanded", cut.Markup);
        Assert.Contains("Transactions analyzed", cut.Markup);
        Assert.Contains("25", cut.Markup);
    }

    /// <summary>
    /// Verifies toggle collapse hides the body.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ToggleCollapse_HidesBody()
    {
        var result = new AnalysisResponseDto();

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        // Click to collapse
        cut.Find(".summary-card-header").Click();

        Assert.Contains("collapsed", cut.Markup);
        Assert.DoesNotContain("summary-card-body", cut.Markup);
    }

    /// <summary>
    /// Verifies the breakdown section shows when there are suggestions.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ShowsBreakdown_WhenSuggestionsExist()
    {
        var result = new AnalysisResponseDto
        {
            NewRuleSuggestions = 3,
            OptimizationSuggestions = 2,
        };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("Suggestions Generated", cut.Markup);
        Assert.Contains("3 new rule(s)", cut.Markup);
        Assert.Contains("2 pattern optimization(s)", cut.Markup);
    }

    /// <summary>
    /// Verifies no-suggestions message when total is zero.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ShowsNoSuggestionsMessage_WhenZero()
    {
        var result = new AnalysisResponseDto
        {
            NewRuleSuggestions = 0,
            OptimizationSuggestions = 0,
            ConflictSuggestions = 0,
        };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("No suggestions needed", cut.Markup);
    }

    /// <summary>
    /// Verifies dismiss button invokes callback.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_DismissButton_InvokesCallback()
    {
        var dismissed = false;
        var result = new AnalysisResponseDto();

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.OnDismiss, () => { dismissed = true; }));

        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        Assert.True(dismissed);
    }

    /// <summary>
    /// Verifies review suggestions button invokes callback.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ReviewButton_InvokesCallback()
    {
        var reviewCalled = false;
        var result = new AnalysisResponseDto { NewRuleSuggestions = 5 };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.OnViewSuggestions, () => { reviewCalled = true; }));

        var reviewBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Review"));
        reviewBtn.Click();

        Assert.True(reviewCalled);
    }

    /// <summary>
    /// Verifies review button is hidden when no suggestions.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_HidesReviewButton_WhenNoSuggestions()
    {
        var result = new AnalysisResponseDto();

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        var buttons = cut.FindAll("button");
        Assert.DoesNotContain(buttons, b => b.TextContent.Contains("Review"));
    }

    /// <summary>
    /// Verifies analysis duration is displayed.
    /// </summary>
    [Fact]
    public void AnalysisSummaryCard_ShowsDuration()
    {
        var result = new AnalysisResponseDto { AnalysisDurationSeconds = 12.5 };

        var cut = Render<AnalysisSummaryCard>(parameters => parameters
            .Add(p => p.Result, result));

        Assert.Contains("12.5s", cut.Markup);
    }
}

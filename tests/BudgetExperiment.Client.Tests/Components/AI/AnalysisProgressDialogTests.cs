// <copyright file="AnalysisProgressDialogTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="AnalysisProgressDialog"/> component.
/// </summary>
public sealed class AnalysisProgressDialogTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisProgressDialogTests"/> class.
    /// </summary>
    public AnalysisProgressDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies progress steps are shown when analysis is running.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsProgressSteps_WhenRunning()
    {
        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, true)
            .Add(p => p.CurrentStep, 2));

        Assert.Contains("analysis-progress", cut.Markup);
        var steps = cut.FindAll(".progress-step");
        Assert.True(steps.Count > 0);
    }

    /// <summary>
    /// Verifies elapsed time is shown when greater than zero.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsElapsedTime_WhenRunning()
    {
        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, true)
            .Add(p => p.CurrentStep, 1)
            .Add(p => p.ElapsedTime, TimeSpan.FromSeconds(15)));

        Assert.Contains("15s", cut.Markup);
    }

    /// <summary>
    /// Verifies completion summary is shown when result is provided.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsCompletionSummary_WhenResultProvided()
    {
        var result = new AnalysisResponseDto
        {
            UncategorizedTransactionsAnalyzed = 50,
            RulesAnalyzed = 10,
            NewRuleSuggestions = 5,
            OptimizationSuggestions = 2,
            ConflictSuggestions = 1,
        };

        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, false)
            .Add(p => p.Result, result));

        Assert.Contains("50", cut.Markup);
        Assert.Contains("10", cut.Markup);
        Assert.Contains("analysis-complete", cut.Markup);
    }

    /// <summary>
    /// Verifies error state is shown when error message is provided.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsError_WhenErrorMessage()
    {
        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, false)
            .Add(p => p.ErrorMessage, "Connection failed"));

        Assert.Contains("Connection failed", cut.Markup);
        Assert.Contains("analysis-error", cut.Markup);
    }

    /// <summary>
    /// Verifies cancel button callback works during analysis.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_CancelButton_InvokesCallback()
    {
        var cancelCalled = false;

        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, true)
            .Add(p => p.CurrentStep, 1)
            .Add(p => p.OnCancel, () => { cancelCalled = true; }));

        var cancelBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        cancelBtn?.Click();

        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies view suggestions button appears after successful completion.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsViewSuggestionsButton_WhenComplete()
    {
        var result = new AnalysisResponseDto
        {
            NewRuleSuggestions = 3,
            OptimizationSuggestions = 1,
        };

        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, false)
            .Add(p => p.Result, result));

        Assert.Contains("View", cut.Markup);
    }

    /// <summary>
    /// Verifies retry button appears on error.
    /// </summary>
    [Fact]
    public void AnalysisProgressDialog_ShowsRetryButton_OnError()
    {
        var retryCalled = false;

        var cut = Render<AnalysisProgressDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsRunning, false)
            .Add(p => p.ErrorMessage, "Timeout")
            .Add(p => p.OnRetry, () => { retryCalled = true; }));

        var retryBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Retry"));
        retryBtn?.Click();

        Assert.True(retryCalled);
    }
}

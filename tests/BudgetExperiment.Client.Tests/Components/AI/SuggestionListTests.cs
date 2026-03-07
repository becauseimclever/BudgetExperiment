// <copyright file="SuggestionListTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="SuggestionList"/> component.
/// </summary>
public sealed class SuggestionListTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionListTests"/> class.
    /// </summary>
    public SuggestionListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies loading state shows spinner.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsLoadingSpinner_WhenIsLoading()
    {
        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.IsLoading, true));

        Assert.Contains("Loading suggestions...", cut.Markup);
    }

    /// <summary>
    /// Verifies empty state when no suggestions exist.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsNoSuggestionsMessage_WhenEmpty()
    {
        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, Array.Empty<RuleSuggestionDto>()));

        Assert.Contains("No Suggestions", cut.Markup);
        Assert.Contains("Run an AI analysis", cut.Markup);
    }

    /// <summary>
    /// Verifies suggestion cards are rendered.
    /// </summary>
    [Fact]
    public void SuggestionList_RendersSuggestionCards()
    {
        var suggestions = new RuleSuggestionDto[]
        {
            CreateSuggestion("NewRule"),
            CreateSuggestion("PatternOptimization"),
        };

        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, suggestions));

        var cards = cut.FindAll(".suggestion-card");
        Assert.Equal(2, cards.Count);
    }

    /// <summary>
    /// Verifies suggestion count is displayed in stats.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsSuggestionCount()
    {
        var suggestions = new RuleSuggestionDto[]
        {
            CreateSuggestion(),
            CreateSuggestion(),
            CreateSuggestion(),
        };

        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, suggestions));

        Assert.Contains("3", cut.Find(".suggestion-stats").TextContent);
        Assert.Contains("suggestion(s)", cut.Find(".suggestion-stats").TextContent);
    }

    /// <summary>
    /// Verifies the type filter dropdown exists with all options.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsTypeFilterDropdown()
    {
        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, new[] { CreateSuggestion() }));

        var options = cut.FindAll("#typeFilter option");
        Assert.True(options.Count >= 6); // All Types + 5 specific types
        Assert.Contains("All Types", cut.Markup);
        Assert.Contains("New Rules", cut.Markup);
        Assert.Contains("Optimizations", cut.Markup);
    }

    /// <summary>
    /// Verifies the sort dropdown exists with all options.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsSortDropdown()
    {
        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, new[] { CreateSuggestion() }));

        var options = cut.FindAll("#sortBy option");
        Assert.True(options.Count >= 5);
        Assert.Contains("Confidence", cut.Markup);
        Assert.Contains("Newest First", cut.Markup);
        Assert.Contains("Most Impact", cut.Markup);
    }

    /// <summary>
    /// Verifies type filter reduces displayed suggestions.
    /// </summary>
    [Fact]
    public void SuggestionList_TypeFilter_FiltersResults()
    {
        var suggestions = new RuleSuggestionDto[]
        {
            CreateSuggestion("NewRule"),
            CreateSuggestion("PatternOptimization"),
            CreateSuggestion("NewRule"),
        };

        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, suggestions));

        // Apply filter
        cut.Find("#typeFilter").Change("NewRule");

        var cards = cut.FindAll(".suggestion-card");
        Assert.Equal(2, cards.Count);
    }

    /// <summary>
    /// Verifies filtered count vs total is shown when filter is applied.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsFilteredVsTotal_WhenFiltered()
    {
        var suggestions = new RuleSuggestionDto[]
        {
            CreateSuggestion("NewRule"),
            CreateSuggestion("PatternOptimization"),
        };

        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, suggestions));

        cut.Find("#typeFilter").Change("NewRule");

        Assert.Contains("of 2 total", cut.Find(".suggestion-stats").TextContent);
    }

    /// <summary>
    /// Verifies no matching filter message when filter removes all results.
    /// </summary>
    [Fact]
    public void SuggestionList_ShowsNoMatchingMessage_WhenFilterEmpty()
    {
        var suggestions = new RuleSuggestionDto[]
        {
            CreateSuggestion("NewRule"),
        };

        var cut = Render<SuggestionList>(parameters => parameters
            .Add(p => p.Suggestions, suggestions));

        cut.Find("#typeFilter").Change("RuleConflict");

        Assert.Contains("No Matching Suggestions", cut.Markup);
    }

    private static RuleSuggestionDto CreateSuggestion(
        string type = "NewRule",
        decimal confidence = 0.9m,
        int impact = 10)
    {
        return new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Type = type,
            Status = "Pending",
            Title = $"Suggestion ({type})",
            Description = "Test description",
            Confidence = confidence,
            AffectedTransactionCount = impact,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

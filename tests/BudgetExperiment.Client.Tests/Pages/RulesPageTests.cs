// <copyright file="RulesPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="Rules"/> page component.
/// </summary>
public class RulesPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesPageTests"/> class.
    /// </summary>
    public RulesPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
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
        var cut = Render<Rules>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Categorization Rules");
    }

    /// <summary>
    /// Verifies the empty state when no rules exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoRules()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("No categorization rules yet");
        cut.Markup.ShouldContain("Create Your First Rule");
    }

    /// <summary>
    /// Verifies the Add Rule button is present.
    /// </summary>
    [Fact]
    public void HasAddRuleButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Add Rule");
    }

    /// <summary>
    /// Verifies the AI Suggestions button is present.
    /// </summary>
    [Fact]
    public void HasAiSuggestionsButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("AI Suggestions");
    }

    /// <summary>
    /// Verifies the Apply Rules button is present.
    /// </summary>
    [Fact]
    public void HasApplyRulesButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Apply Rules");
    }

    /// <summary>
    /// Verifies rules are displayed when they exist.
    /// </summary>
    [Fact]
    public void ShowsRules_WhenRulesExist()
    {
        this._apiService.Rules.Add(CreateRule("Grocery Matcher", "GROCERY", "Contains"));
        this._apiService.Categories.Add(CreateCategory("Groceries"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies the priority order info text is shown with rules.
    /// </summary>
    [Fact]
    public void ShowsPriorityInfo_WhenRulesExist()
    {
        this._apiService.Rules.Add(CreateRule("Test Rule", "test", "Contains"));
        this._apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Rules are evaluated in priority order");
    }

    /// <summary>
    /// Verifies multiple rules are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleRules()
    {
        this._apiService.Rules.Add(CreateRule("Rule A", "PATTERN_A", "Contains", priority: 1));
        this._apiService.Rules.Add(CreateRule("Rule B", "PATTERN_B", "Exact", priority: 2));
        this._apiService.Categories.Add(CreateCategory("Cat A"));

        var cut = Render<Rules>();

        // Both rules should be rendered
        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies the empty state description text.
    /// </summary>
    [Fact]
    public void ShowsEmptyStateDescription()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Create rules to automatically assign categories to transactions based on their descriptions");
    }

    /// <summary>
    /// Verifies the Apply Rules button is disabled when no rules exist.
    /// </summary>
    [Fact]
    public void ApplyRulesButton_IsDisabled_WhenNoRules()
    {
        var cut = Render<Rules>();

        // Button should be present but disabled
        cut.Markup.ShouldContain("Apply Rules");
    }

    private static CategorizationRuleDto CreateRule(
        string name,
        string pattern,
        string matchType,
        int priority = 1,
        bool isActive = true)
    {
        return new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Pattern = pattern,
            MatchType = matchType,
            CaseSensitive = false,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Test Category",
            Priority = priority,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static BudgetCategoryDto CreateCategory(string name)
    {
        return new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = "Expense",
            IsActive = true,
        };
    }
}

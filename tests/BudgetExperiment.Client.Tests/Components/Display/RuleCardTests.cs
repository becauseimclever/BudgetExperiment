// <copyright file="RuleCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="RuleCard"/> component.
/// </summary>
public class RuleCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleCardTests"/> class.
    /// </summary>
    public RuleCardTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies rule name is displayed.
    /// </summary>
    [Fact]
    public void DisplaysRuleName()
    {
        var cut = RenderCard(name: "Grocery Rule");

        Assert.Contains("Grocery Rule", cut.Markup);
    }

    /// <summary>
    /// Verifies pattern is displayed in code format.
    /// </summary>
    [Fact]
    public void DisplaysPatternInCode()
    {
        var cut = RenderCard(pattern: "WALMART");

        var code = cut.Find("code.rule-pattern");
        Assert.Contains("WALMART", code.TextContent);
    }

    /// <summary>
    /// Verifies priority badge is displayed.
    /// </summary>
    [Fact]
    public void DisplaysPriorityBadge()
    {
        var cut = RenderCard(priority: 3);

        Assert.Contains("#3", cut.Markup);
    }

    /// <summary>
    /// Verifies match type badge is displayed.
    /// </summary>
    [Fact]
    public void DisplaysMatchTypeBadge()
    {
        var cut = RenderCard(matchType: "Contains");

        Assert.Contains("Contains", cut.Markup);
    }

    /// <summary>
    /// Verifies match type badge CSS class for different types.
    /// </summary>
    /// <param name="matchType">The match type to test.</param>
    /// <param name="expectedClass">The expected CSS class.</param>
    [Theory]
    [InlineData("Exact", "badge-primary")]
    [InlineData("Contains", "badge-info")]
    [InlineData("StartsWith", "badge-info")]
    [InlineData("Regex", "badge-warning")]
    [InlineData("Unknown", "badge-secondary")]
    public void MatchTypeBadge_HasCorrectClass(string matchType, string expectedClass)
    {
        var cut = RenderCard(matchType: matchType);

        Assert.Contains(expectedClass, cut.Markup);
    }

    /// <summary>
    /// Verifies case sensitivity indicator is shown when true.
    /// </summary>
    [Fact]
    public void CaseSensitive_ShowsIndicator()
    {
        var cut = RenderCard(caseSensitive: true);

        Assert.Contains("Aa", cut.Markup);
    }

    /// <summary>
    /// Verifies case sensitivity indicator is hidden when false.
    /// </summary>
    [Fact]
    public void NotCaseSensitive_HidesIndicator()
    {
        var cut = RenderCard(caseSensitive: false);

        // The "Aa" badge should not appear when case-insensitive
        var badges = cut.FindAll(".rule-details .badge");
        Assert.DoesNotContain(badges, b => b.TextContent.Trim() == "Aa");
    }

    /// <summary>
    /// Verifies target category name is displayed.
    /// </summary>
    [Fact]
    public void DisplaysTargetCategoryName()
    {
        var cut = RenderCard(categoryName: "Food");

        var target = cut.Find(".category-name");
        Assert.Contains("Food", target.TextContent);
    }

    /// <summary>
    /// Verifies "Unknown Category" fallback when category is null.
    /// </summary>
    [Fact]
    public void NullCategoryName_ShowsUnknownCategory()
    {
        var cut = RenderCard(categoryName: null);

        Assert.Contains("Unknown Category", cut.Markup);
    }

    /// <summary>
    /// Verifies inactive rule shows inactive badge.
    /// </summary>
    [Fact]
    public void InactiveRule_ShowsInactiveBadge()
    {
        var cut = RenderCard(isActive: false);

        Assert.Contains("Inactive", cut.Markup);
        Assert.Contains("card-inactive", cut.Markup);
    }

    /// <summary>
    /// Verifies active rule shows deactivate button.
    /// </summary>
    [Fact]
    public void ActiveRule_ShowsDeactivateButton()
    {
        var cut = RenderCard(isActive: true);

        var deactivateBtn = cut.Find("button[title='Deactivate rule']");
        Assert.NotNull(deactivateBtn);
    }

    /// <summary>
    /// Verifies inactive rule shows activate button.
    /// </summary>
    [Fact]
    public void InactiveRule_ShowsActivateButton()
    {
        var cut = RenderCard(isActive: false);

        var activateBtn = cut.Find("button[title='Activate rule']");
        Assert.NotNull(activateBtn);
    }

    /// <summary>
    /// Verifies edit button fires OnEdit callback.
    /// </summary>
    [Fact]
    public void EditButton_FiresCallback()
    {
        CategorizationRuleDto? edited = null;
        var rule = CreateRule(name: "My Rule");

        var cut = Render<RuleCard>(p => p
            .Add(x => x.Rule, rule)
            .Add(x => x.OnEdit, (CategorizationRuleDto r) =>
            {
                edited = r;
                return Task.CompletedTask;
            }));

        var editBtn = cut.Find("button[title='Edit rule']");
        editBtn.Click();

        Assert.NotNull(edited);
        Assert.Equal("My Rule", edited!.Name);
    }

    /// <summary>
    /// Verifies delete button fires OnDelete callback.
    /// </summary>
    [Fact]
    public void DeleteButton_FiresCallback()
    {
        CategorizationRuleDto? deleted = null;
        var rule = CreateRule();

        var cut = Render<RuleCard>(p => p
            .Add(x => x.Rule, rule)
            .Add(x => x.OnDelete, (CategorizationRuleDto r) =>
            {
                deleted = r;
                return Task.CompletedTask;
            }));

        var deleteBtn = cut.Find("button[title='Delete rule']");
        deleteBtn.Click();

        Assert.NotNull(deleted);
    }

    /// <summary>
    /// Verifies deactivate button fires OnDeactivate callback.
    /// </summary>
    [Fact]
    public void DeactivateButton_FiresCallback()
    {
        CategorizationRuleDto? deactivated = null;
        var rule = CreateRule(isActive: true);

        var cut = Render<RuleCard>(p => p
            .Add(x => x.Rule, rule)
            .Add(x => x.OnDeactivate, (CategorizationRuleDto r) =>
            {
                deactivated = r;
                return Task.CompletedTask;
            }));

        var deactivateBtn = cut.Find("button[title='Deactivate rule']");
        deactivateBtn.Click();

        Assert.NotNull(deactivated);
    }

    /// <summary>
    /// Verifies activate button fires OnActivate callback.
    /// </summary>
    [Fact]
    public void ActivateButton_FiresCallback()
    {
        CategorizationRuleDto? activated = null;
        var rule = CreateRule(isActive: false);

        var cut = Render<RuleCard>(p => p
            .Add(x => x.Rule, rule)
            .Add(x => x.OnActivate, (CategorizationRuleDto r) =>
            {
                activated = r;
                return Task.CompletedTask;
            }));

        var activateBtn = cut.Find("button[title='Activate rule']");
        activateBtn.Click();

        Assert.NotNull(activated);
    }

    private static CategorizationRuleDto CreateRule(
        string name = "Test Rule",
        string pattern = "test",
        string matchType = "Contains",
        bool caseSensitive = false,
        int priority = 1,
        string? categoryName = "Test Category",
        bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Pattern = pattern,
            MatchType = matchType,
            CaseSensitive = caseSensitive,
            Priority = priority,
            CategoryId = Guid.NewGuid(),
            CategoryName = categoryName,
            IsActive = isActive,
        };

    private IRenderedComponent<RuleCard> RenderCard(
        string name = "Test Rule",
        string pattern = "test",
        string matchType = "Contains",
        bool caseSensitive = false,
        int priority = 1,
        string? categoryName = "Test Category",
        bool isActive = true)
    {
        var rule = CreateRule(name, pattern, matchType, caseSensitive, priority, categoryName, isActive);
        return Render<RuleCard>(p => p
            .Add(x => x.Rule, rule));
    }
}

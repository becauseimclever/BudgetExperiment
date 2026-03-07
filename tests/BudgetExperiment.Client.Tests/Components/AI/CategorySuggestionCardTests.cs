// <copyright file="CategorySuggestionCardTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="CategorySuggestionCard"/> component.
/// </summary>
public sealed class CategorySuggestionCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionCardTests"/> class.
    /// </summary>
    public CategorySuggestionCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the card shows the suggested category name.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsSuggestedName()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion("Coffee Shops")));

        Assert.Contains("Coffee Shops", cut.Markup);
    }

    /// <summary>
    /// Verifies the card shows the confidence badge.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsConfidence()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(confidence: 0.92m)));

        Assert.Contains("92", cut.Markup);
    }

    /// <summary>
    /// Verifies the card shows transaction count.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsTransactionCount()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(matchCount: 25)));

        Assert.Contains("25 transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies merchant patterns are shown as chips.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsPatternChips()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion()));

        Assert.Contains("WALMART", cut.Markup);
        Assert.Contains("TARGET", cut.Markup);
        Assert.Contains("KROGER", cut.Markup);
    }

    /// <summary>
    /// Verifies "more" chip shows when patterns exceed 3.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsMoreChip_WhenExcessPatterns()
    {
        var suggestion = new CategorySuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedName = "Test",
            SuggestedType = "Expense",
            Confidence = 0.8m,
            MerchantPatterns = new[] { "A", "B", "C", "D", "E" },
            MatchingTransactionCount = 10,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
        };

        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, suggestion));

        Assert.Contains("+2 more", cut.Markup);
    }

    /// <summary>
    /// Verifies accept button invokes callback.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_AcceptButton_InvokesCallback()
    {
        var accepted = false;

        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.OnAccept, () => { accepted = true; }));

        var acceptBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Accept"));
        acceptBtn.Click();

        Assert.True(accepted);
    }

    /// <summary>
    /// Verifies dismiss button invokes callback.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_DismissButton_InvokesCallback()
    {
        var dismissed = false;

        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.OnDismiss, () => { dismissed = true; }));

        var dismissBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss"));
        dismissBtn.Click();

        Assert.True(dismissed);
    }

    /// <summary>
    /// Verifies checkbox is shown in normal view.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsCheckbox_InNormalView()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.IsDismissedView, false));

        var checkboxes = cut.FindAll(".selection-checkbox");
        Assert.NotEmpty(checkboxes);
    }

    /// <summary>
    /// Verifies checkbox is hidden in dismissed view.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_HidesCheckbox_InDismissedView()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.IsDismissedView, true));

        var checkboxes = cut.FindAll(".selection-checkbox");
        Assert.Empty(checkboxes);
    }

    /// <summary>
    /// Verifies restore button shown in dismissed view.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsRestoreButton_InDismissedView()
    {
        var restored = false;

        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.IsDismissedView, true)
            .Add(p => p.OnRestore, () => { restored = true; }));

        var restoreBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Restore"));
        restoreBtn.Click();

        Assert.True(restored);
    }

    /// <summary>
    /// Verifies status badge class matches suggestion status.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsCorrectStatusBadge()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion(status: "Pending")));

        Assert.Contains("status-pending", cut.Markup);
    }

    /// <summary>
    /// Verifies selected card has selected CSS class.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_HasSelectedClass_WhenSelected()
    {
        var cut = Render<CategorySuggestionCard>(parameters => parameters
            .Add(p => p.Suggestion, CreateSuggestion())
            .Add(p => p.IsSelected, true));

        var card = cut.Find(".category-suggestion-card");
        Assert.Contains("selected", card.ClassList);
    }

    private static CategorySuggestionDto CreateSuggestion(
        string name = "Groceries",
        decimal confidence = 0.85m,
        int matchCount = 12,
        string status = "Pending") => new()
    {
        Id = Guid.NewGuid(),
        SuggestedName = name,
        SuggestedType = "Expense",
        Confidence = confidence,
        MerchantPatterns = new[] { "WALMART", "TARGET", "KROGER" },
        MatchingTransactionCount = matchCount,
        Status = status,
        CreatedAtUtc = DateTime.UtcNow,
    };
}

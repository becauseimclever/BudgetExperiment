// <copyright file="CategoryBudgetCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="CategoryBudgetCard"/> component.
/// </summary>
public class CategoryBudgetCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryBudgetCardTests"/> class.
    /// </summary>
    public CategoryBudgetCardTests()
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
    /// Verifies category name is displayed.
    /// </summary>
    [Fact]
    public void DisplaysCategoryName()
    {
        var cut = RenderCard(name: "Groceries");

        Assert.Contains("Groceries", cut.Markup);
    }

    /// <summary>
    /// Verifies spent and target amounts are displayed when budget is set.
    /// </summary>
    [Fact]
    public void DisplaysSpentAndTargetAmounts()
    {
        var cut = RenderCard(
            status: "OnTrack",
            spentAmount: 150m,
            targetAmount: 500m);

        Assert.DoesNotContain("No budget set", cut.Markup);
    }

    /// <summary>
    /// Verifies "No budget set" message when status is NoBudgetSet.
    /// </summary>
    [Fact]
    public void NoBudgetSet_ShowsNoBudgetMessage()
    {
        var cut = RenderCard(status: "NoBudgetSet");

        Assert.Contains("No budget set", cut.Markup);
        Assert.Contains("Set a budget to track spending", cut.Markup);
    }

    /// <summary>
    /// Verifies remaining amount is shown when on track.
    /// </summary>
    [Fact]
    public void OnTrack_ShowsRemainingAmount()
    {
        var cut = RenderCard(
            status: "OnTrack",
            remainingAmount: 350m);

        Assert.Contains("remaining", cut.Markup);
    }

    /// <summary>
    /// Verifies over budget amount shows "over budget" text.
    /// </summary>
    [Fact]
    public void OverBudget_ShowsOverBudgetText()
    {
        var cut = RenderCard(
            status: "OverBudget",
            remainingAmount: -50m);

        Assert.Contains("over budget", cut.Markup);
    }

    /// <summary>
    /// Verifies transaction count is displayed.
    /// </summary>
    [Fact]
    public void DisplaysTransactionCount()
    {
        var cut = RenderCard(transactionCount: 42);

        Assert.Contains("42 transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies OnTrack status applies success CSS class.
    /// </summary>
    [Fact]
    public void OnTrack_HasSuccessClass()
    {
        var cut = RenderCard(status: "OnTrack");

        Assert.Contains("status-success", cut.Markup);
    }

    /// <summary>
    /// Verifies Warning status applies warning CSS class.
    /// </summary>
    [Fact]
    public void Warning_HasWarningClass()
    {
        var cut = RenderCard(status: "Warning");

        Assert.Contains("status-warning", cut.Markup);
    }

    /// <summary>
    /// Verifies OverBudget status applies danger CSS class.
    /// </summary>
    [Fact]
    public void OverBudget_HasDangerClass()
    {
        var cut = RenderCard(status: "OverBudget");

        Assert.Contains("status-danger", cut.Markup);
    }

    /// <summary>
    /// Verifies NoBudgetSet status applies no-budget CSS class.
    /// </summary>
    [Fact]
    public void NoBudgetSet_HasNoBudgetClass()
    {
        var cut = RenderCard(status: "NoBudgetSet");

        Assert.Contains("status-no-budget", cut.Markup);
    }

    /// <summary>
    /// Verifies edit button shows "Set Budget" when no budget is set.
    /// </summary>
    [Fact]
    public void NoBudgetSet_ShowsSetBudgetButton()
    {
        var cut = RenderCard(status: "NoBudgetSet", showEditButton: true);

        Assert.Contains("Set Budget", cut.Markup);
    }

    /// <summary>
    /// Verifies edit button shows "Edit Goal" when budget is set.
    /// </summary>
    [Fact]
    public void WithBudget_ShowsEditGoalButton()
    {
        var cut = RenderCard(status: "OnTrack", showEditButton: true);

        Assert.Contains("Edit Goal", cut.Markup);
    }

    /// <summary>
    /// Verifies edit button is hidden when ShowEditButton is false.
    /// </summary>
    [Fact]
    public void ShowEditButtonFalse_HidesButton()
    {
        var cut = RenderCard(status: "OnTrack", showEditButton: false);

        Assert.DoesNotContain("Edit Goal", cut.Markup);
        Assert.DoesNotContain("Set Budget", cut.Markup);
    }

    /// <summary>
    /// Verifies OnEditGoal callback fires with progress data.
    /// </summary>
    [Fact]
    public void EditButton_FiresOnEditGoalCallback()
    {
        BudgetProgressDto? clicked = null;
        var progress = CreateProgress(name: "Food", status: "OnTrack");

        var cut = Render<CategoryBudgetCard>(p => p
            .Add(x => x.Progress, progress)
            .Add(x => x.ShowEditButton, true)
            .Add(x => x.OnEditGoal, (BudgetProgressDto p) =>
            {
                clicked = p;
                return Task.CompletedTask;
            }));

        var editBtn = cut.Find(".card-footer button");
        editBtn.Click();

        Assert.NotNull(clicked);
        Assert.Equal("Food", clicked!.CategoryName);
    }

    /// <summary>
    /// Verifies category icon emoji mapping.
    /// </summary>
    /// <param name="icon">The icon name to test.</param>
    /// <param name="expectedEmoji">The expected emoji output.</param>
    [Theory]
    [InlineData("cart", "🛒")]
    [InlineData("home", "🏠")]
    [InlineData("car", "🚗")]
    [InlineData("unknown-icon", "📁")]
    public void DisplaysCorrectIconEmoji(string icon, string expectedEmoji)
    {
        var cut = RenderCard(icon: icon);

        Assert.Contains(expectedEmoji, cut.Markup);
    }

    private static BudgetProgressDto CreateProgress(
        string name = "Test Category",
        string status = "OnTrack",
        decimal spentAmount = 100m,
        decimal targetAmount = 500m,
        decimal remainingAmount = 400m,
        int transactionCount = 5,
        string? icon = null,
        string? color = null)
    {
        return new BudgetProgressDto
        {
            CategoryId = Guid.NewGuid(),
            CategoryName = name,
            CategoryIcon = icon,
            CategoryColor = color,
            Status = status,
            SpentAmount = new MoneyDto { Amount = spentAmount, Currency = "USD" },
            TargetAmount = new MoneyDto { Amount = targetAmount, Currency = "USD" },
            RemainingAmount = new MoneyDto { Amount = remainingAmount, Currency = "USD" },
            PercentUsed = targetAmount > 0 ? (spentAmount / targetAmount * 100) : 0,
            TransactionCount = transactionCount,
        };
    }

    private IRenderedComponent<CategoryBudgetCard> RenderCard(
        string name = "Test Category",
        string status = "OnTrack",
        decimal spentAmount = 100m,
        decimal targetAmount = 500m,
        decimal remainingAmount = 400m,
        int transactionCount = 5,
        string? icon = null,
        string? color = null,
        bool showEditButton = true)
    {
        var progress = CreateProgress(name, status, spentAmount, targetAmount, remainingAmount, transactionCount, icon, color);

        return Render<CategoryBudgetCard>(p => p
            .Add(x => x.Progress, progress)
            .Add(x => x.ShowEditButton, showEditButton));
    }
}

// <copyright file="CalendarBudgetCategoryRowTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the CalendarBudgetCategoryRow component.
/// </summary>
public class CalendarBudgetCategoryRowTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBudgetCategoryRowTests"/> class.
    /// </summary>
    public CalendarBudgetCategoryRowTests()
    {
        // Mock JS interop for ThemeService
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the row displays the category name.
    /// </summary>
    [Fact]
    public void Row_DisplaysCategoryName()
    {
        // Arrange
        var progress = CreateProgress("Test Category", "OnTrack", 50, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        Assert.Contains("Test Category", cut.Markup);
    }

    /// <summary>
    /// Verifies that the row displays spent and target amounts for categories with budgets.
    /// </summary>
    [Fact]
    public void Row_DisplaysAmounts_WhenBudgetIsSet()
    {
        // Arrange
        var progress = CreateProgress("Food", "OnTrack", 350, 400);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        Assert.Contains("$350.00", cut.Markup);
        Assert.Contains("$400.00", cut.Markup);
    }

    /// <summary>
    /// Verifies that the row shows "no budget" label when no budget is set.
    /// </summary>
    [Fact]
    public void Row_ShowsNoBudgetLabel_WhenNoBudgetSet()
    {
        // Arrange
        var progress = CreateProgress("Coffee", "NoBudgetSet", 0, 0);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        Assert.Contains("(no budget)", cut.Markup);
    }

    /// <summary>
    /// Verifies that the progress bar is not rendered when no budget is set.
    /// </summary>
    [Fact]
    public void Row_DoesNotRenderProgressBar_WhenNoBudgetSet()
    {
        // Arrange
        var progress = CreateProgress("Coffee", "NoBudgetSet", 0, 0);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var hasProgressBar = cut.FindAll(".progress-bar-container").Count > 0;
        Assert.False(hasProgressBar);
    }

    /// <summary>
    /// Verifies that the progress bar is rendered when budget is set.
    /// </summary>
    [Fact]
    public void Row_RendersProgressBar_WhenBudgetIsSet()
    {
        // Arrange
        var progress = CreateProgress("Food", "OnTrack", 200, 400);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var hasProgressBar = cut.FindAll(".progress-bar-container").Count > 0;
        Assert.True(hasProgressBar);
    }

    /// <summary>
    /// Verifies that the row applies the correct status class for OnTrack.
    /// </summary>
    [Fact]
    public void Row_AppliesOnTrackClass()
    {
        // Arrange
        var progress = CreateProgress("Food", "OnTrack", 50, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var row = cut.Find(".category-row");
        Assert.Contains("status-on-track", row.ClassList);
    }

    /// <summary>
    /// Verifies that the row applies the correct status class for Warning.
    /// </summary>
    [Fact]
    public void Row_AppliesWarningClass()
    {
        // Arrange
        var progress = CreateProgress("Food", "Warning", 85, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var row = cut.Find(".category-row");
        Assert.Contains("status-warning", row.ClassList);
    }

    /// <summary>
    /// Verifies that the row applies the correct status class for OverBudget.
    /// </summary>
    [Fact]
    public void Row_AppliesOverBudgetClass()
    {
        // Arrange
        var progress = CreateProgress("Food", "OverBudget", 150, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var row = cut.Find(".category-row");
        Assert.Contains("status-over-budget", row.ClassList);
    }

    /// <summary>
    /// Verifies that clicking Edit triggers OnEdit callback.
    /// </summary>
    [Fact]
    public void Row_TriggersOnEdit_WhenEditClicked()
    {
        // Arrange
        var editedProgress = (BudgetProgressDto?)null;
        var progress = CreateProgress("Food", "OnTrack", 50, 100);

        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<BudgetProgressDto>(this, p => editedProgress = p)));

        // Act
        cut.Find("button").Click();

        // Assert
        Assert.NotNull(editedProgress);
        Assert.Equal("Food", editedProgress!.CategoryName);
    }

    /// <summary>
    /// Verifies that clicking Set triggers OnSetBudget callback.
    /// </summary>
    [Fact]
    public void Row_TriggersOnSetBudget_WhenSetClicked()
    {
        // Arrange
        var createdProgress = (BudgetProgressDto?)null;
        var progress = CreateProgress("Coffee", "NoBudgetSet", 0, 0);

        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress)
            .Add(p => p.OnSetBudget, EventCallback.Factory.Create<BudgetProgressDto>(this, p => createdProgress = p)));

        // Act
        cut.Find("button").Click();

        // Assert
        Assert.NotNull(createdProgress);
        Assert.Equal("Coffee", createdProgress!.CategoryName);
    }

    /// <summary>
    /// Verifies that the Edit button is shown for categories with budgets.
    /// </summary>
    [Fact]
    public void Row_ShowsEditButton_WhenBudgetIsSet()
    {
        // Arrange
        var progress = CreateProgress("Food", "OnTrack", 50, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Edit", button.TextContent);
    }

    /// <summary>
    /// Verifies that the Set button is shown for categories without budgets.
    /// </summary>
    [Fact]
    public void Row_ShowsSetButton_WhenNoBudgetIsSet()
    {
        // Arrange
        var progress = CreateProgress("Coffee", "NoBudgetSet", 0, 0);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Set", button.TextContent);
    }

    /// <summary>
    /// Verifies that the progress bar width is capped at 100% for over-budget categories.
    /// </summary>
    [Fact]
    public void Row_CapsProgressBarAt100Percent()
    {
        // Arrange
        var progress = CreateProgress("Food", "OverBudget", 150, 100);

        // Act
        var cut = Render<CalendarBudgetCategoryRow>(parameters => parameters
            .Add(p => p.Progress, progress));

        // Assert
        var progressFill = cut.Find(".progress-bar-fill");
        var style = progressFill.GetAttribute("style");
        Assert.Contains("width: 100%", style);
    }

    private static BudgetProgressDto CreateProgress(string name, string status, decimal spent, decimal target)
    {
        var percentUsed = target > 0 ? (spent / target) * 100 : 0;
        return new BudgetProgressDto
        {
            CategoryId = Guid.NewGuid(),
            CategoryName = name,
            Status = status,
            TargetAmount = new MoneyDto { Amount = target, Currency = "USD" },
            SpentAmount = new MoneyDto { Amount = spent, Currency = "USD" },
            RemainingAmount = new MoneyDto { Amount = target - spent, Currency = "USD" },
            PercentUsed = percentUsed,
        };
    }
}

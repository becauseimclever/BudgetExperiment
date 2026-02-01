// <copyright file="CalendarBudgetPanelTests.cs" company="BecauseImClever">
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
/// Unit tests for the CalendarBudgetPanel component.
/// </summary>
public class CalendarBudgetPanelTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBudgetPanelTests"/> class.
    /// </summary>
    public CalendarBudgetPanelTests()
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
    /// Verifies that the panel does not render when Summary is null.
    /// </summary>
    [Fact]
    public void Panel_DoesNotRender_WhenSummaryIsNull()
    {
        // Arrange & Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, null)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the panel renders with expanded class by default.
    /// </summary>
    [Fact]
    public void Panel_RendersExpandedByDefault()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        var panel = cut.Find(".calendar-budget-panel");
        Assert.Contains("expanded", panel.ClassList);
    }

    /// <summary>
    /// Verifies that clicking the header toggles the panel state.
    /// </summary>
    [Fact]
    public void Panel_TogglesOnHeaderClick()
    {
        // Arrange
        var summary = CreateTestSummary();
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act - Click to collapse
        cut.Find(".panel-header").Click();

        // Assert - Now collapsed
        var panel = cut.Find(".calendar-budget-panel");
        Assert.Contains("collapsed", panel.ClassList);
        Assert.DoesNotContain("expanded", panel.ClassList);

        // Act - Click to expand again
        cut.Find(".panel-header").Click();

        // Assert - Expanded again
        panel = cut.Find(".calendar-budget-panel");
        Assert.Contains("expanded", panel.ClassList);
    }

    /// <summary>
    /// Verifies that the panel displays the correct month name.
    /// </summary>
    [Fact]
    public void Panel_DisplaysCorrectMonthName()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("February 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel displays the correct total spent and budgeted.
    /// </summary>
    [Fact]
    public void Panel_DisplaysTotalSpentAndBudgeted()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.TotalSpent = new MoneyDto { Amount = 500m, Currency = "USD" };
        summary.TotalBudgeted = new MoneyDto { Amount = 1000m, Currency = "USD" };

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("$500.00", cut.Markup);
        Assert.Contains("$1,000.00", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel displays the correct overall percent used.
    /// </summary>
    [Fact]
    public void Panel_DisplaysOverallPercentUsed()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.OverallPercentUsed = 50;

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("50%", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel displays status counts when present.
    /// </summary>
    [Fact]
    public void Panel_DisplaysStatusCounts()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.CategoriesOnTrack = 3;
        summary.CategoriesWarning = 1;
        summary.CategoriesOverBudget = 2;

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("3 on track", cut.Markup);
        Assert.Contains("1 warning", cut.Markup);
        Assert.Contains("2 over budget", cut.Markup);
    }

    /// <summary>
    /// Verifies that categories are sorted by status priority.
    /// </summary>
    [Fact]
    public void Panel_SortsCategoriesByStatus()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.CategoryProgress =
        [
            new BudgetProgressDto { CategoryId = Guid.NewGuid(), CategoryName = "OnTrack Cat", Status = "OnTrack", TargetAmount = new MoneyDto { Amount = 100, Currency = "USD" }, SpentAmount = new MoneyDto { Amount = 50, Currency = "USD" }, PercentUsed = 50 },
            new BudgetProgressDto { CategoryId = Guid.NewGuid(), CategoryName = "OverBudget Cat", Status = "OverBudget", TargetAmount = new MoneyDto { Amount = 100, Currency = "USD" }, SpentAmount = new MoneyDto { Amount = 150, Currency = "USD" }, PercentUsed = 150 },
            new BudgetProgressDto { CategoryId = Guid.NewGuid(), CategoryName = "Warning Cat", Status = "Warning", TargetAmount = new MoneyDto { Amount = 100, Currency = "USD" }, SpentAmount = new MoneyDto { Amount = 85, Currency = "USD" }, PercentUsed = 85 },
        ];

        // Act
        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert - OverBudget should appear before Warning, which should appear before OnTrack
        var rows = cut.FindAll(".category-row");
        Assert.Equal(3, rows.Count);

        // Check order by finding the category names in order
        var markup = cut.Markup;
        var overBudgetIndex = markup.IndexOf("OverBudget Cat", StringComparison.Ordinal);
        var warningIndex = markup.IndexOf("Warning Cat", StringComparison.Ordinal);
        var onTrackIndex = markup.IndexOf("OnTrack Cat", StringComparison.Ordinal);

        Assert.True(overBudgetIndex < warningIndex, "OverBudget should appear before Warning");
        Assert.True(warningIndex < onTrackIndex, "Warning should appear before OnTrack");
    }

    /// <summary>
    /// Verifies that OnEditGoal callback is triggered when edit is clicked.
    /// </summary>
    [Fact]
    public void Panel_TriggersOnEditGoal_WhenEditClicked()
    {
        // Arrange
        var editedProgress = (BudgetProgressDto?)null;
        var summary = CreateTestSummary();
        summary.CategoryProgress =
        [
            new BudgetProgressDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Test Category",
                Status = "OnTrack",
                TargetAmount = new MoneyDto { Amount = 100, Currency = "USD" },
                SpentAmount = new MoneyDto { Amount = 50, Currency = "USD" },
                PercentUsed = 50,
            },
        ];

        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.OnEditGoal, EventCallback.Factory.Create<BudgetProgressDto>(this, p => editedProgress = p)));

        // Act - Find and click the Edit button
        var editButton = cut.Find(".category-actions button");
        editButton.Click();

        // Assert
        Assert.NotNull(editedProgress);
        Assert.Equal("Test Category", editedProgress!.CategoryName);
    }

    /// <summary>
    /// Verifies that OnCreateGoal callback is triggered when Set button is clicked.
    /// </summary>
    [Fact]
    public void Panel_TriggersOnCreateGoal_WhenSetClicked()
    {
        // Arrange
        var createdProgress = (BudgetProgressDto?)null;
        var summary = CreateTestSummary();
        summary.CategoryProgress =
        [
            new BudgetProgressDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "No Budget Category",
                Status = "NoBudgetSet",
                TargetAmount = new MoneyDto { Amount = 0, Currency = "USD" },
                SpentAmount = new MoneyDto { Amount = 100, Currency = "USD" },
                PercentUsed = 0,
            },
        ];

        var cut = Render<CalendarBudgetPanel>(parameters => parameters
            .Add(p => p.Summary, summary)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.OnCreateGoal, EventCallback.Factory.Create<BudgetProgressDto>(this, p => createdProgress = p)));

        // Act - Find and click the Set button
        var setButton = cut.Find(".category-actions button");
        setButton.Click();

        // Assert
        Assert.NotNull(createdProgress);
        Assert.Equal("No Budget Category", createdProgress!.CategoryName);
    }

    private static BudgetSummaryDto CreateTestSummary()
    {
        return new BudgetSummaryDto
        {
            Year = 2026,
            Month = 2,
            TotalBudgeted = new MoneyDto { Amount = 1000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 500m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 500m, Currency = "USD" },
            OverallPercentUsed = 50m,
            CategoriesOnTrack = 2,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };
    }
}

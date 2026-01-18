// <copyright file="BudgetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Infrastructure;
using BudgetExperiment.E2E.Tests.PageObjects;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests for budget overview functionality.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class BudgetTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright _fixture.</param>
    public BudgetTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the Budget page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task BudgetPage_LoadsSuccessfully()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/budget");

        var budgetPage = new BudgetPage(page);
        await budgetPage.WaitForLoadingCompleteAsync();

        // Page should load without errors
        var hasError = await budgetPage.HasErrorAsync();
        Assert.False(hasError, "Budget page should load without errors");
    }

    /// <summary>
    /// Verifies the month navigation buttons exist.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task BudgetPage_HasMonthNavigationButtons()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/budget");

        var budgetPage = new BudgetPage(page);
        await budgetPage.WaitForLoadingCompleteAsync();

        var hasPrevButton = await budgetPage.PreviousMonthButton.IsVisibleAsync();
        var hasNextButton = await budgetPage.NextMonthButton.IsVisibleAsync();

        Assert.True(hasPrevButton, "Previous month button should be visible");
        Assert.True(hasNextButton, "Next month button should be visible");
    }

    /// <summary>
    /// Verifies navigating to previous month updates the display.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ClickPreviousMonth_UpdatesMonthDisplay()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/budget");

        var budgetPage = new BudgetPage(page);
        await budgetPage.WaitForLoadingCompleteAsync();

        var initialMonth = await budgetPage.GetCurrentMonthTextAsync();
        await budgetPage.GoToPreviousMonthAsync();
        var newMonth = await budgetPage.GetCurrentMonthTextAsync();

        Assert.NotEqual(initialMonth, newMonth);
    }

    /// <summary>
    /// Verifies navigating to next month updates the display.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ClickNextMonth_UpdatesMonthDisplay()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/budget");

        var budgetPage = new BudgetPage(page);
        await budgetPage.WaitForLoadingCompleteAsync();

        // First go to previous month so we can go forward
        await budgetPage.GoToPreviousMonthAsync();
        var initialMonth = await budgetPage.GetCurrentMonthTextAsync();

        await budgetPage.GoToNextMonthAsync();
        var newMonth = await budgetPage.GetCurrentMonthTextAsync();

        Assert.NotEqual(initialMonth, newMonth);
    }

    /// <summary>
    /// Verifies the current month display shows valid month name.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task BudgetPage_DisplaysCurrentMonth()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/budget");

        var budgetPage = new BudgetPage(page);
        await budgetPage.WaitForLoadingCompleteAsync();

        var monthText = await budgetPage.GetCurrentMonthTextAsync();

        // Should contain a month name
        var monthNames = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        var containsMonthName = monthNames.Any(m => monthText.Contains(m, StringComparison.OrdinalIgnoreCase));

        Assert.True(containsMonthName, $"Month display '{monthText}' should contain a month name");
    }
}

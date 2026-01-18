// <copyright file="NavigationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Infrastructure;
using BudgetExperiment.E2E.Tests.PageObjects;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests for navigation between pages.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class NavigationTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright _fixture.</param>
    public NavigationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the home page loads and redirects to accounts.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task HomePage_LoadsSuccessfully()
    {
        var page = await _fixture.CreatePageAsync();

        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        // The app should load without errors
        var errorAlert = page.Locator(".alert-danger, .error-alert");
        var hasError = await errorAlert.IsVisibleAsync();

        Assert.False(hasError, "Page should load without errors");
    }

    /// <summary>
    /// Verifies navigation to the Accounts page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToAccounts_DisplaysAccountsPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var accountsPage = await basePage.Navigation.GoToAccountsAsync();

        await accountsPage.WaitForLoadingCompleteAsync();

        // Verify we're on the accounts page
        await page.WaitForURLAsync("**/accounts");
        var title = await page.TitleAsync();
        Assert.Contains("Account", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Budget page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToBudget_DisplaysBudgetPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var budgetPage = await basePage.Navigation.GoToBudgetAsync();

        await budgetPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/budget");
        var title = await page.TitleAsync();
        Assert.Contains("Budget", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Calendar page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToCalendar_DisplaysCalendarPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var calendarPage = await basePage.Navigation.GoToCalendarAsync();

        await calendarPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/calendar");
        var title = await page.TitleAsync();
        Assert.Contains("Calendar", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Categories page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToCategories_DisplaysCategoriesPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var categoriesPage = await basePage.Navigation.GoToCategoriesAsync();

        await categoriesPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/categories");
        var title = await page.TitleAsync();
        Assert.Contains("Categor", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Recurring page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToRecurring_DisplaysRecurringPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var recurringPage = await basePage.Navigation.GoToRecurringAsync();

        await recurringPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/recurring");
        var title = await page.TitleAsync();
        Assert.Contains("Recurring", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Rules page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToRules_DisplaysRulesPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var rulesPage = await basePage.Navigation.GoToRulesAsync();

        await rulesPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/rules");
        var title = await page.TitleAsync();
        Assert.Contains("Rule", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies navigation to the Settings page works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateToSettings_DisplaysSettingsPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var basePage = new AccountsPage(page);
        var settingsPage = await basePage.Navigation.GoToSettingsAsync();

        await settingsPage.WaitForLoadingCompleteAsync();

        await page.WaitForURLAsync("**/settings");
        var title = await page.TitleAsync();
        Assert.Contains("Setting", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that all main navigation links are present.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task Sidebar_ContainsAllExpectedNavigationLinks()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var nav = new NavigationComponent(page);
        var links = await nav.GetAllNavLinksAsync();

        Assert.Contains(links, l => l.Contains("Account", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(links, l => l.Contains("Budget", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(links, l => l.Contains("Calendar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(links, l => l.Contains("Categor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(links, l => l.Contains("Recurring", StringComparison.OrdinalIgnoreCase));
    }
}

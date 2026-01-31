// <copyright file="NavigationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests to verify all main pages are accessible.
/// </summary>
[Collection("Playwright")]
public class NavigationTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public NavigationTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the Calendar (home) page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task CalendarPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync(string.Empty, "Calendar");
    }

    /// <summary>
    /// Verifies the Recurring Bills page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task RecurringBillsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("recurring", "Recurring Bills");
    }

    /// <summary>
    /// Verifies the Auto-Transfers page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task AutoTransfersPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("recurring-transfers", "Auto-Transfers");
    }

    /// <summary>
    /// Verifies the Reconciliation page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task ReconciliationPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("reconciliation", "Reconciliation");
    }

    /// <summary>
    /// Verifies the Transfers page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task TransfersPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("transfers", "Transfers");
    }

    /// <summary>
    /// Verifies the Paycheck Planner page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task PaycheckPlannerPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("paycheck-planner", "Paycheck Planner");
    }

    /// <summary>
    /// Verifies the Categories page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task CategoriesPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("categories", "Budget Categories");
    }

    /// <summary>
    /// Verifies the Auto-Categorize (Rules) page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task RulesPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("rules", "Auto-Categorize");
    }

    /// <summary>
    /// Verifies the Budget page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task BudgetPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("budget", "Budget Overview");
    }

    /// <summary>
    /// Verifies the Reports Overview page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task ReportsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("reports", "Reports Overview");
    }

    /// <summary>
    /// Verifies the Category Spending report page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task CategorySpendingReportPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("reports/categories", "Category Spending");
    }

    /// <summary>
    /// Verifies the Accounts page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task AccountsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("accounts", "All Accounts");
    }

    /// <summary>
    /// Verifies the Import page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task ImportPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("import", "Import Transactions");
    }

    /// <summary>
    /// Verifies the Smart Insights (AI Suggestions) page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task SmartInsightsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("ai/suggestions", "Smart Insights");
    }

    /// <summary>
    /// Verifies the Category Suggestions page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task CategorySuggestionsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("category-suggestions", "Category Suggestions");
    }

    /// <summary>
    /// Verifies the Settings page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "DemoSafe")]
    public async Task SettingsPage_ShouldLoad()
    {
        await NavigateAndVerifyPageAsync("settings", "Settings");
    }

    private async Task NavigateAndVerifyPageAsync(string route, string pageName)
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        var targetUrl = string.IsNullOrEmpty(route)
            ? fixture.BaseUrl
            : $"{fixture.BaseUrl.TrimEnd('/')}/{route}";
        var response = await page.GotoAsync(targetUrl);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"{pageName} page should return OK, got {response.Status}");

        // Verify we're on the correct URL
        Assert.Contains(route, page.Url.Replace(fixture.BaseUrl.TrimEnd('/'), string.Empty));

        // Verify the page has loaded (navigation still visible, no error state)
        var navMenu = page.Locator("nav.nav-menu");
        await Expect(navMenu).ToBeVisibleAsync(new() { Timeout = 5000 });
    }
}

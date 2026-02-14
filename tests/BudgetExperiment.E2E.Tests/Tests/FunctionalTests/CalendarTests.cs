// <copyright file="CalendarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;
namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Functional E2E tests for calendar navigation and running balance display behavior.
/// </summary>
[Collection("Playwright")]
public class CalendarTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public CalendarTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies calendar month view loads and displays day balance cells.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_ShouldLoadCurrentMonthWithDayBalances()
    {
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var now = DateTime.UtcNow;
        await page.GotoAsync($"{_fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToBeVisibleAsync(new() { Timeout = 15000 });

        var dayBalanceCount = await page.Locator(".calendar-day .day-balance").CountAsync();
        Assert.True(dayBalanceCount >= 28, $"Expected at least 28 day balance cells, got {dayBalanceCount}");
    }

    /// <summary>
    /// Verifies previous/next month navigation updates calendar heading.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_MonthNavigation_ShouldChangeDisplayedMonth()
    {
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var now = DateTime.UtcNow;
        await page.GotoAsync($"{_fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });
        var heading = page.GetByRole(AriaRole.Heading, new() { Level = 2 });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 15000 });
        var originalHeading = await heading.TextContentAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Next >" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var nextHeading = await heading.TextContentAsync();
        Assert.NotEqual(originalHeading, nextHeading);

        await page.GetByRole(AriaRole.Button, new() { Name = "< Previous" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var returnedHeading = await heading.TextContentAsync();
        Assert.Equal(originalHeading, returnedHeading);
    }

    /// <summary>
    /// Verifies transaction count indicators are accompanied by day totals.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_TransactionDay_ShouldShowCountAndTotal()
    {
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var now = DateTime.UtcNow;
        await page.GotoAsync($"{_fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });

        var dayWithTransactions = page.Locator(".calendar-day", new() { Has = page.Locator(".transaction-count") }).First;
        var transactionDayCount = await page.Locator(".calendar-day .transaction-count").CountAsync();

        if (transactionDayCount == 0)
        {
            return;
        }

        await Expect(dayWithTransactions.Locator(".transaction-count")).ToBeVisibleAsync();
        await Expect(dayWithTransactions.Locator(".day-total")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies account switching triggers a filtered calendar data request.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_AccountFilter_ShouldReloadCalendarForSelectedAccount()
    {
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var now = DateTime.UtcNow;
        await page.GotoAsync($"{_fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });

        var accountSelect = page.Locator("#accountSelect");
        await Expect(accountSelect).ToBeVisibleAsync(new() { Timeout = 15000 });

        var options = await accountSelect.Locator("option").AllInnerTextsAsync();
        if (options.Count < 2)
        {
            return;
        }

        var selectedValue = await accountSelect.Locator("option").Nth(1).GetAttributeAsync("value");
        Assert.False(string.IsNullOrWhiteSpace(selectedValue));

        var response = await page.RunAndWaitForResponseAsync(
            async () =>
            {
                await accountSelect.SelectOptionAsync(new[] { selectedValue! });
            },
            r => r.Url.Contains("/api/v1/calendar/grid", StringComparison.OrdinalIgnoreCase)
                 && r.Url.Contains($"accountId={selectedValue}", StringComparison.OrdinalIgnoreCase)
                 && r.Status == 200);

        Assert.NotNull(response);
        await Expect(accountSelect).ToHaveValueAsync(selectedValue!);
    }

    /// <summary>
    /// Verifies account transaction view displays starting balance and running balance values.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task AccountTransactions_ShouldShowStartingAndRunningBalances()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var transactionsButton = page.GetByRole(AriaRole.Button, new() { Name = "Transactions" }).First;
        await Expect(transactionsButton).ToBeVisibleAsync(new() { Timeout = 10000 });
        await transactionsButton.ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.GetByText("Starting Balance:")).ToBeVisibleAsync();
        await Expect(page.GetByText("Current Balance:")).ToBeVisibleAsync();

        var runningBalanceCell = page.Locator("td.running-balance").First;
        await Expect(runningBalanceCell).ToBeVisibleAsync();

        var balanceText = await runningBalanceCell.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(balanceText));
        Assert.NotEqual("-", balanceText.Trim());
    }
}

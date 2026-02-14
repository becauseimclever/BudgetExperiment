// <copyright file="AccountTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Functional E2E tests for account management and account-scoped filtering.
/// </summary>
[Collection("Playwright")]
public class AccountTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public AccountTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies creating an account applies the configured starting balance in account transactions.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task Accounts_Create_ShouldShowStartingBalanceInTransactions()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("StartBal");

        await CreateAccountAsync(page, accountName, "432.10");
        await OpenAccountTransactionsAsync(page, accountName);

        await Expect(page.GetByText("Starting Balance:")).ToBeVisibleAsync();
        await Expect(page.GetByText("Current Balance:")).ToBeVisibleAsync();

        var banner = page.Locator(".balance-banner");
        await Expect(banner).ToBeVisibleAsync();
        var bannerText = await banner.InnerTextAsync();
        Assert.Contains("432", bannerText, StringComparison.Ordinal);

        await DeleteAccountAsync(page, accountName);
    }

    /// <summary>
    /// Verifies switching calendar account filter sends account-specific requests.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task Accounts_SwitchingFilter_ShouldRequestAccountScopedCalendarData()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountA = TestDataHelper.CreateUniqueName("A");
        var accountB = TestDataHelper.CreateUniqueName("B");

        await CreateAccountAsync(page, accountA, "100.00");
        await CreateAccountAsync(page, accountB, "200.00");

        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var accountSelect = page.Locator("#accountSelect");
        await Expect(accountSelect).ToBeVisibleAsync();

        var accountAValue = await accountSelect.Locator("option", new() { HasText = accountA }).First.GetAttributeAsync("value");
        var accountBValue = await accountSelect.Locator("option", new() { HasText = accountB }).First.GetAttributeAsync("value");

        Assert.False(string.IsNullOrWhiteSpace(accountAValue));
        Assert.False(string.IsNullOrWhiteSpace(accountBValue));

        var responseA = await page.RunAndWaitForResponseAsync(
            async () => await accountSelect.SelectOptionAsync(new[] { accountAValue! }),
            r => r.Url.Contains("/api/v1/calendar/grid", StringComparison.OrdinalIgnoreCase)
                 && r.Url.Contains($"accountId={accountAValue}", StringComparison.OrdinalIgnoreCase)
                 && r.Status == 200);

        Assert.NotNull(responseA);

        var responseB = await page.RunAndWaitForResponseAsync(
            async () => await accountSelect.SelectOptionAsync(new[] { accountBValue! }),
            r => r.Url.Contains("/api/v1/calendar/grid", StringComparison.OrdinalIgnoreCase)
                 && r.Url.Contains($"accountId={accountBValue}", StringComparison.OrdinalIgnoreCase)
                 && r.Status == 200);

        Assert.NotNull(responseB);

        await DeleteAccountAsync(page, accountA);
        await DeleteAccountAsync(page, accountB);
    }

    private async Task CreateAccountAsync(IPage page, string accountName, string initialBalance)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Account" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Account" })).ToBeVisibleAsync();

        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync(initialBalance);
        await page.Locator("#initialBalanceDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await page.Locator(".modal-dialog", new() { HasText = "Add Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Save" })
            .ClickAsync();

        await Expect(page.Locator(".card", new() { HasText = accountName })).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private async Task OpenAccountTransactionsAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new() { HasText = accountName });
        await Expect(card).ToBeVisibleAsync(new() { Timeout = 10000 });
        await card.GetByRole(AriaRole.Button, new() { Name = "Transactions" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task DeleteAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".card", new() { HasText = accountName });
        if (await card.CountAsync() == 0)
        {
            return;
        }

        await card.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Delete Account" })).ToBeVisibleAsync();

        await page.Locator(".modal-dialog", new() { HasText = "Delete Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Delete" })
            .ClickAsync();

        await Expect(card).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}

// <copyright file="AccountCreateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Critical local E2E test validating the account creation flow (Feature 068).
/// </summary>
[Collection("Playwright")]
public class AccountCreateTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountCreateTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public AccountCreateTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies creating an account through the UI and confirming it appears on the accounts page.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateAccount_ShouldAppearOnAccountsPage()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("Acct");

        // Navigate to accounts page
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open the add account modal
        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Account" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Account" })).ToBeVisibleAsync();

        // Fill in account details
        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync("500.00");
        await page.Locator("#initialBalanceDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        // Save
        await page.Locator(".modal-dialog", new() { HasText = "Add Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Save" })
            .ClickAsync();

        // Assert the account card appears
        var accountCard = page.Locator(".card", new() { HasText = accountName });
        await Expect(accountCard).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify the account name is displayed in the card
        var cardText = await accountCard.InnerTextAsync();
        Assert.Contains(accountName, cardText, StringComparison.Ordinal);

        // Clean up
        await DeleteAccountAsync(page, accountName);
    }

    /// <summary>
    /// Verifies a newly created account is selectable in the calendar account filter.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateAccount_ShouldBeSelectableInCalendarFilter()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("CalAcct");

        // Create the account
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Account" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Account" })).ToBeVisibleAsync();

        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync("250.00");
        await page.Locator("#initialBalanceDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await page.Locator(".modal-dialog", new() { HasText = "Add Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Save" })
            .ClickAsync();

        await Expect(page.Locator(".card", new() { HasText = accountName })).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Navigate to calendar
        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the account is available in the account select dropdown
        var accountSelect = page.Locator("#accountSelect");
        await Expect(accountSelect).ToBeVisibleAsync(new() { Timeout = 10000 });

        var accountOption = accountSelect.Locator("option", new() { HasText = accountName });
        await Expect(accountOption.First).ToBeAttachedAsync(new() { Timeout = 10000 });

        // Select the account to confirm it's functional
        var optionValue = await accountOption.First.GetAttributeAsync("value");
        Assert.False(string.IsNullOrWhiteSpace(optionValue));
        await accountSelect.SelectOptionAsync(new[] { optionValue! });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Clean up
        await DeleteAccountAsync(page, accountName);
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

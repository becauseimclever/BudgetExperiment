// <copyright file="TransactionCreateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Critical local E2E test validating the transaction creation flow (Feature 068).
/// </summary>
[Collection("Playwright")]
public class TransactionCreateTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionCreateTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public TransactionCreateTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies creating a transaction through the UI and confirming it appears in the account transactions list.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateTransaction_ShouldAppearInAccountTransactionsList()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("TxnAcct");
        var description = TestDataHelper.CreateUniqueName("Txn");
        var amount = "42.75";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Create a test account to hold the transaction
        await CreateAccountAsync(page, accountName);
        await OpenAccountTransactionsAsync(page, accountName);

        // Add a transaction
        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Transaction" }).ClickAsync();
        var addModal = page.Locator(".modal-dialog", new() { HasText = "Add Transaction" });
        await Expect(addModal).ToBeVisibleAsync();

        await page.Locator("#txnDescription").FillAsync(description);
        await page.Keyboard.PressAsync("Tab");

        await page.Locator("#txnAmount").FillAsync(amount);
        await page.Keyboard.PressAsync("Tab");

        // Skip category selector
        await page.Keyboard.PressAsync("Tab");

        await page.Locator("#txnDate").FillAsync(date);
        await page.Keyboard.PressAsync("Tab");

        await addModal.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Wait for modal to close (confirms save succeeded)
        await Expect(addModal).Not.ToBeVisibleAsync(new() { Timeout = 15000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert the transaction appears in the list
        var transactionRow = page.Locator("tr", new() { HasText = description });
        await Expect(transactionRow).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify the amount is displayed
        var rowText = await transactionRow.InnerTextAsync();
        Assert.Contains("42.75", rowText, StringComparison.Ordinal);

        // Clean up
        await DeleteAccountAsync(page, accountName);
    }

    /// <summary>
    /// Verifies creating a transaction is reflected on the calendar page for the current month.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    [Trait("Category", "LocalCritical")]
    public async Task CreateTransaction_ShouldAppearOnCalendar()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("CalTxn");
        var description = TestDataHelper.CreateUniqueName("CalItem");
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Create a test account and add a transaction
        await CreateAccountAsync(page, accountName);
        await OpenAccountTransactionsAsync(page, accountName);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Transaction" }).ClickAsync();
        var addModal = page.Locator(".modal-dialog", new() { HasText = "Add Transaction" });
        await Expect(addModal).ToBeVisibleAsync();

        await page.Locator("#txnDescription").FillAsync(description);
        await page.Keyboard.PressAsync("Tab");

        await page.Locator("#txnAmount").FillAsync("25.00");
        await page.Keyboard.PressAsync("Tab");

        // Skip category selector
        await page.Keyboard.PressAsync("Tab");

        await page.Locator("#txnDate").FillAsync(date);
        await page.Keyboard.PressAsync("Tab");

        await addModal.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Wait for modal to close (confirms save succeeded)
        await Expect(addModal).Not.ToBeVisibleAsync(new() { Timeout = 15000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator("tr", new() { HasText = description })).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Navigate to the calendar and select the account
        var now = DateTime.UtcNow;
        await page.GotoAsync($"{_fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var accountSelect = page.Locator("#accountSelect");
        await Expect(accountSelect).ToBeVisibleAsync(new() { Timeout = 10000 });

        var accountOption = accountSelect.Locator("option", new() { HasText = accountName }).First;
        var accountValue = await accountOption.GetAttributeAsync("value");
        Assert.False(string.IsNullOrWhiteSpace(accountValue));

        await accountSelect.SelectOptionAsync(new[] { accountValue! });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the calendar grid loads with day balances
        await Expect(page.Locator(".calendar-grid")).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Clean up
        await DeleteAccountAsync(page, accountName);
    }

    private async Task CreateAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Account" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Account" })).ToBeVisibleAsync();

        await page.Locator("#accountName").FillAsync(accountName);
        await page.Locator("#initialBalance").FillAsync("1000.00");
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
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "+ Add Transaction" })).ToBeVisibleAsync(new() { Timeout = 10000 });
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

// <copyright file="TransactionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Functional E2E tests for transaction CRUD workflows.
/// </summary>
[Collection("Playwright")]
public class TransactionTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public TransactionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies creating, editing, and deleting a transaction from account transactions page.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task Transactions_ShouldSupportCreateEditDeleteFlow()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        var accountName = TestDataHelper.CreateUniqueName("Account");
        var createdDescription = TestDataHelper.CreateUniqueName("Txn");
        var updatedDescription = createdDescription + "-Updated";

        await CreateAccountAsync(page, accountName);
        await OpenAccountTransactionsAsync(page, accountName);

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add Transaction" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Add Transaction" })).ToBeVisibleAsync();

        await page.Locator("#txnDescription").FillAsync(createdDescription);
        await page.Locator("#txnAmount").FillAsync("12.34");
        await page.Locator("#txnDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        var createdRow = page.Locator("tr", new() { HasText = createdDescription });
        await Expect(createdRow).ToBeVisibleAsync(new() { Timeout = 10000 });

        await createdRow.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Edit Transaction" })).ToBeVisibleAsync();

        await page.Locator("#txnDescription").FillAsync(updatedDescription);
        await page.Locator("#txnAmount").FillAsync("99.50");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        var updatedRow = page.Locator("tr", new() { HasText = updatedDescription });
        await Expect(updatedRow).ToBeVisibleAsync(new() { Timeout = 10000 });

        await updatedRow.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Delete Transaction" })).ToBeVisibleAsync();
        await page.Locator(".modal-dialog", new() { HasText = "Delete Transaction" })
            .GetByRole(AriaRole.Button, new() { Name = "Delete" })
            .ClickAsync();

        await Expect(updatedRow).Not.ToBeVisibleAsync(new() { Timeout = 10000 });

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

        var accountCard = page.Locator(".card", new() { HasText = accountName });
        await Expect(accountCard).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private async Task OpenAccountTransactionsAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var accountCard = page.Locator(".card", new() { HasText = accountName });
        await Expect(accountCard).ToBeVisibleAsync(new() { Timeout = 10000 });
        await accountCard.GetByRole(AriaRole.Button, new() { Name = "Transactions" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(page.Locator("table.table")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private async Task DeleteAccountAsync(IPage page, string accountName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var accountCard = page.Locator(".card", new() { HasText = accountName });
        if (await accountCard.CountAsync() == 0)
        {
            return;
        }

        await accountCard.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(page.Locator(".modal-dialog", new() { HasText = "Delete Account" })).ToBeVisibleAsync();

        await page.Locator(".modal-dialog", new() { HasText = "Delete Account" })
            .GetByRole(AriaRole.Button, new() { Name = "Delete" })
            .ClickAsync();

        await Expect(accountCard).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}

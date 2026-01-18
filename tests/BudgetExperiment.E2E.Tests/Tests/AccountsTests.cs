// <copyright file="AccountsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Infrastructure;
using BudgetExperiment.E2E.Tests.PageObjects;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests for account management functionality.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class AccountsTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright _fixture.</param>
    public AccountsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the Accounts page loads and displays content.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task AccountsPage_LoadsSuccessfully()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/accounts");

        var accountsPage = new AccountsPage(page);
        await accountsPage.WaitForLoadingCompleteAsync();

        // Should have either accounts or empty message
        var hasAccounts = await accountsPage.GetAccountCountAsync() > 0;
        var hasEmptyMessage = await accountsPage.EmptyMessage.IsVisibleAsync();

        Assert.True(hasAccounts || hasEmptyMessage, "Page should show accounts or empty message");
    }

    /// <summary>
    /// Verifies the Add Account button is visible.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task AccountsPage_HasAddAccountButton()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/accounts");

        var accountsPage = new AccountsPage(page);
        await accountsPage.WaitForLoadingCompleteAsync();

        var isVisible = await accountsPage.AddAccountButton.IsVisibleAsync();
        Assert.True(isVisible, "Add Account button should be visible");
    }

    /// <summary>
    /// Verifies clicking Add Account opens the modal.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ClickAddAccount_OpensModal()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/accounts");

        var accountsPage = new AccountsPage(page);
        await accountsPage.WaitForLoadingCompleteAsync();

        await accountsPage.ClickAddAccountAsync();

        var modalVisible = await accountsPage.AddAccountModal.IsVisibleAsync();
        Assert.True(modalVisible, "Add Account modal should be visible");
    }

    /// <summary>
    /// Verifies creating a new account works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task CreateAccount_AppearsInList()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/accounts");

        var accountsPage = new AccountsPage(page);
        await accountsPage.WaitForLoadingCompleteAsync();

        var accountName = $"Test Account {DateTime.UtcNow:HHmmss}";
        await accountsPage.CreateAccountAsync(accountName, "Checking", 1000.00m);

        // Wait for the list to refresh
        await accountsPage.WaitForLoadingCompleteAsync();

        // Verify account appears in list
        var accountCard = accountsPage.GetAccountCardByName(accountName);
        var isVisible = await accountCard.IsVisibleAsync();
        Assert.True(isVisible, $"Account '{accountName}' should appear in the list");

        // Cleanup: delete the account
        await accountsPage.DeleteAccountAsync(accountName);
    }

    /// <summary>
    /// Verifies clicking Transactions navigates to account transactions.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ClickTransactions_NavigatesToAccountTransactionsPage()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/accounts");

        var accountsPage = new AccountsPage(page);
        await accountsPage.WaitForLoadingCompleteAsync();

        // First, ensure we have at least one account
        var accountCount = await accountsPage.GetAccountCountAsync();
        if (accountCount == 0)
        {
            var accountName = $"Test Account {DateTime.UtcNow:HHmmss}";
            await accountsPage.CreateAccountAsync(accountName, "Checking", 500.00m);
            await accountsPage.WaitForLoadingCompleteAsync();
        }

        // Get the first account card and click transactions
        var firstAccountCard = accountsPage.AccountCards.First;
        var accountName2 = await firstAccountCard.Locator(".card-title").TextContentAsync();
        await firstAccountCard.GetByRole(AriaRole.Button, new() { Name = "Transactions" }).ClickAsync();

        // Verify navigation
        await page.WaitForURLAsync("**/accounts/*/transactions");
        var transactionsPage = new AccountTransactionsPage(page);
        await transactionsPage.WaitForLoadingCompleteAsync();

        // Verify we're on the transactions page
        var url = page.Url;
        Assert.Contains("/transactions", url);
    }
}

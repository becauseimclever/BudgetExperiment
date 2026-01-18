// <copyright file="AccountsPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Accounts page.
/// </summary>
public class AccountsPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public AccountsPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Add Account button.
    /// </summary>
    public ILocator AddAccountButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add Account" });

    /// <summary>
    /// Gets the Transfer button.
    /// </summary>
    public ILocator TransferButton => Page.GetByRole(AriaRole.Button, new() { Name = "Transfer" });

    /// <summary>
    /// Gets all account cards.
    /// </summary>
    public ILocator AccountCards => Page.Locator(".card");

    /// <summary>
    /// Gets the empty state message.
    /// </summary>
    public ILocator EmptyMessage => Page.Locator(".empty-message");

    /// <summary>
    /// Gets the add account modal.
    /// </summary>
    public ModalComponent AddAccountModal => new(Page, "Add Account");

    /// <summary>
    /// Clicks the Add Account button to open the modal.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickAddAccountAsync()
    {
        await AddAccountButton.ClickAsync();
        await AddAccountModal.WaitForVisibleAsync();
    }

    /// <summary>
    /// Creates a new account with the given details.
    /// </summary>
    /// <param name="name">Account name.</param>
    /// <param name="type">Account type.</param>
    /// <param name="initialBalance">Initial balance.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateAccountAsync(string name, string type, decimal initialBalance)
    {
        await ClickAddAccountAsync();

        await Page.GetByLabel("Account Name").FillAsync(name);
        await Page.GetByLabel("Account Type").SelectOptionAsync(type);
        await Page.GetByLabel("Initial Balance").FillAsync(initialBalance.ToString());

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Account" }).ClickAsync();

        // Wait for modal to close
        await AddAccountModal.WaitForHiddenAsync();
    }

    /// <summary>
    /// Gets the account card by name.
    /// </summary>
    /// <param name="name">Account name to find.</param>
    /// <returns>The account card locator.</returns>
    public ILocator GetAccountCardByName(string name)
    {
        return Page.Locator(".card").Filter(new() { HasText = name });
    }

    /// <summary>
    /// Clicks the Transactions button on an account card.
    /// </summary>
    /// <param name="accountName">The account name.</param>
    /// <returns>The AccountTransactionsPage.</returns>
    public async Task<AccountTransactionsPage> ViewTransactionsAsync(string accountName)
    {
        var card = GetAccountCardByName(accountName);
        await card.GetByRole(AriaRole.Button, new() { Name = "Transactions" }).ClickAsync();
        await Page.WaitForURLAsync("**/accounts/*/transactions");
        return new AccountTransactionsPage(Page);
    }

    /// <summary>
    /// Gets the count of account cards displayed.
    /// </summary>
    /// <returns>The number of account cards.</returns>
    public async Task<int> GetAccountCountAsync()
    {
        return await AccountCards.CountAsync();
    }

    /// <summary>
    /// Deletes an account by name.
    /// </summary>
    /// <param name="accountName">The account name to delete.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteAccountAsync(string accountName)
    {
        var card = GetAccountCardByName(accountName);
        await card.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        // Handle confirmation if present
        var confirmButton = Page.GetByRole(AriaRole.Button, new() { Name = "Confirm" });
        if (await confirmButton.IsVisibleAsync())
        {
            await confirmButton.ClickAsync();
        }
    }
}

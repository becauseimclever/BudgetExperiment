// <copyright file="AccountTransactionsPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Account Transactions page.
/// </summary>
public class AccountTransactionsPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountTransactionsPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public AccountTransactionsPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Add Transaction button.
    /// </summary>
    public ILocator AddTransactionButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add Transaction" });

    /// <summary>
    /// Gets the Import button.
    /// </summary>
    public ILocator ImportButton => Page.GetByRole(AriaRole.Button, new() { Name = "Import" });

    /// <summary>
    /// Gets the transactions table.
    /// </summary>
    public ILocator TransactionsTable => Page.Locator("table");

    /// <summary>
    /// Gets all transaction rows.
    /// </summary>
    public ILocator TransactionRows => Page.Locator("table tbody tr");

    /// <summary>
    /// Gets the back button to return to accounts.
    /// </summary>
    public ILocator BackButton => Page.GetByRole(AriaRole.Link, new() { Name = "Back" });

    /// <summary>
    /// Gets the running balance display.
    /// </summary>
    public ILocator RunningBalance => Page.Locator("[class*='balance'], .running-balance");

    /// <summary>
    /// Gets the count of transaction rows.
    /// </summary>
    /// <returns>The number of transaction rows.</returns>
    public async Task<int> GetTransactionCountAsync()
    {
        return await TransactionRows.CountAsync();
    }

    /// <summary>
    /// Clicks the Add Transaction button to open the modal.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickAddTransactionAsync()
    {
        await AddTransactionButton.ClickAsync();
        var modal = new ModalComponent(Page, "Add Transaction");
        await modal.WaitForVisibleAsync();
    }

    /// <summary>
    /// Creates a new transaction with the given details.
    /// </summary>
    /// <param name="description">Transaction description.</param>
    /// <param name="amount">Transaction amount.</param>
    /// <param name="date">Optional transaction date.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateTransactionAsync(string description, decimal amount, DateOnly? date = null)
    {
        await ClickAddTransactionAsync();

        await Page.GetByLabel("Description").FillAsync(description);
        await Page.GetByLabel("Amount").FillAsync(amount.ToString());

        if (date.HasValue)
        {
            await Page.GetByLabel("Date").FillAsync(date.Value.ToString("yyyy-MM-dd"));
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        var modal = new ModalComponent(Page, "Add Transaction");
        await modal.WaitForHiddenAsync();
    }

    /// <summary>
    /// Finds a transaction row by description.
    /// </summary>
    /// <param name="description">The transaction description to find.</param>
    /// <returns>The transaction row locator.</returns>
    public ILocator GetTransactionByDescription(string description)
    {
        return TransactionRows.Filter(new() { HasText = description });
    }

    /// <summary>
    /// Checks if a transaction with the given description exists.
    /// </summary>
    /// <param name="description">The transaction description to find.</param>
    /// <returns>True if the transaction exists.</returns>
    public async Task<bool> HasTransactionAsync(string description)
    {
        return await GetTransactionByDescription(description).CountAsync() > 0;
    }

    /// <summary>
    /// Deletes a transaction by description.
    /// </summary>
    /// <param name="description">The transaction description to delete.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteTransactionAsync(string description)
    {
        var row = GetTransactionByDescription(description);
        await row.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        // Handle confirmation if present
        var confirmButton = Page.GetByRole(AriaRole.Button, new() { Name = "Confirm" });
        if (await confirmButton.IsVisibleAsync())
        {
            await confirmButton.ClickAsync();
        }
    }
}

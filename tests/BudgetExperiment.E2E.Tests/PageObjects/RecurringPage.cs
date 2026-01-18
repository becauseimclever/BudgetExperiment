// <copyright file="RecurringPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Recurring Transactions page.
/// </summary>
public class RecurringPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public RecurringPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Add Recurring button.
    /// </summary>
    public ILocator AddRecurringButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add Recurring" });

    /// <summary>
    /// Gets all recurring transaction items.
    /// </summary>
    public ILocator RecurringItems => Page.Locator(".recurring-item, .card, table tbody tr");

    /// <summary>
    /// Gets the empty message.
    /// </summary>
    public ILocator EmptyMessage => Page.Locator(".empty-message");

    /// <summary>
    /// Gets the count of recurring transactions.
    /// </summary>
    /// <returns>The number of recurring items.</returns>
    public async Task<int> GetRecurringCountAsync()
    {
        return await RecurringItems.CountAsync();
    }

    /// <summary>
    /// Finds a recurring transaction by description.
    /// </summary>
    /// <param name="description">The recurring transaction description.</param>
    /// <returns>The recurring item locator.</returns>
    public ILocator GetRecurringByDescription(string description)
    {
        return RecurringItems.Filter(new() { HasText = description });
    }
}

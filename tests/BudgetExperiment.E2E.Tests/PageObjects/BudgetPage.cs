// <copyright file="BudgetPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Budget Overview page.
/// </summary>
public class BudgetPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public BudgetPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the previous month button.
    /// </summary>
    public ILocator PreviousMonthButton => Page.Locator("button[title='Previous month']");

    /// <summary>
    /// Gets the next month button.
    /// </summary>
    public ILocator NextMonthButton => Page.Locator("button[title='Next month']");

    /// <summary>
    /// Gets the current month display.
    /// </summary>
    public ILocator CurrentMonthDisplay => Page.Locator(".current-month");

    /// <summary>
    /// Gets the budget summary card.
    /// </summary>
    public ILocator SummaryCard => Page.Locator(".budget-summary-card");

    /// <summary>
    /// Gets the total budgeted amount.
    /// </summary>
    public ILocator TotalBudgeted => Page.Locator(".stat-label:has-text('Total Budgeted') + .stat-value");

    /// <summary>
    /// Gets the total spent amount.
    /// </summary>
    public ILocator TotalSpent => Page.Locator(".stat-label:has-text('Total Spent') + .stat-value");

    /// <summary>
    /// Gets the remaining amount.
    /// </summary>
    public ILocator Remaining => Page.Locator(".stat-label:has-text('Remaining') + .stat-value");

    /// <summary>
    /// Gets all category progress items.
    /// </summary>
    public ILocator CategoryItems => Page.Locator(".budget-category-item, .category-progress");

    /// <summary>
    /// Navigates to the previous month.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task GoToPreviousMonthAsync()
    {
        await PreviousMonthButton.ClickAsync();
        await WaitForLoadingCompleteAsync();
    }

    /// <summary>
    /// Navigates to the next month.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task GoToNextMonthAsync()
    {
        await NextMonthButton.ClickAsync();
        await WaitForLoadingCompleteAsync();
    }

    /// <summary>
    /// Gets the current month text.
    /// </summary>
    /// <returns>The current month display text.</returns>
    public async Task<string> GetCurrentMonthTextAsync()
    {
        return await CurrentMonthDisplay.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Gets the count of category items displayed.
    /// </summary>
    /// <returns>The number of category items.</returns>
    public async Task<int> GetCategoryCountAsync()
    {
        return await CategoryItems.CountAsync();
    }
}

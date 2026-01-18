// <copyright file="NavigationComponent.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Infrastructure;

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the sidebar navigation component.
/// </summary>
public class NavigationComponent
{
    private readonly IPage _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationComponent"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public NavigationComponent(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Gets the sidebar locator.
    /// </summary>
    public ILocator Sidebar => _page.Locator(".app-sidebar").First;

    /// <summary>
    /// Navigates to the Accounts page.
    /// </summary>
    /// <returns>The Accounts page object.</returns>
    public async Task<AccountsPage> GoToAccountsAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Accounts" }).ClickAsync();
        await _page.WaitForURLAsync("**/accounts");
        return new AccountsPage(_page);
    }

    /// <summary>
    /// Navigates to the Budget page.
    /// </summary>
    /// <returns>The Budget page object.</returns>
    public async Task<BudgetPage> GoToBudgetAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Budget" }).ClickAsync();
        await _page.WaitForURLAsync("**/budget");
        return new BudgetPage(_page);
    }

    /// <summary>
    /// Navigates to the Calendar page.
    /// </summary>
    /// <returns>The Calendar page object.</returns>
    public async Task<CalendarPage> GoToCalendarAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Calendar" }).ClickAsync();
        await _page.WaitForURLAsync("**/calendar");
        return new CalendarPage(_page);
    }

    /// <summary>
    /// Navigates to the Categories page.
    /// </summary>
    /// <returns>The Categories page object.</returns>
    public async Task<CategoriesPage> GoToCategoriesAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Categories" }).ClickAsync();
        await _page.WaitForURLAsync("**/categories");
        return new CategoriesPage(_page);
    }

    /// <summary>
    /// Navigates to the Recurring page.
    /// </summary>
    /// <returns>The Recurring page object.</returns>
    public async Task<RecurringPage> GoToRecurringAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Recurring" }).ClickAsync();
        await _page.WaitForURLAsync("**/recurring");
        return new RecurringPage(_page);
    }

    /// <summary>
    /// Navigates to the Rules page.
    /// </summary>
    /// <returns>The Rules page object.</returns>
    public async Task<RulesPage> GoToRulesAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Rules" }).ClickAsync();
        await _page.WaitForURLAsync("**/rules");
        return new RulesPage(_page);
    }

    /// <summary>
    /// Navigates to the Settings page.
    /// </summary>
    /// <returns>The Settings page object.</returns>
    public async Task<SettingsPage> GoToSettingsAsync()
    {
        await EnsureAppIsLoadedAsync();
        await _page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await _page.WaitForURLAsync("**/settings");
        return new SettingsPage(_page);
    }

    /// <summary>
    /// Gets all navigation link texts.
    /// </summary>
    /// <returns>List of navigation link texts.</returns>
    public async Task<IReadOnlyList<string>> GetAllNavLinksAsync()
    {
        await EnsureAppIsLoadedAsync();
        var links = _page.Locator(".app-sidebar a");
        var count = await links.CountAsync();
        var result = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var text = await links.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add(text.Trim());
            }
        }

        return result;
    }

    /// <summary>
    /// Ensures the app is fully loaded by handling any pending auth and waiting for navigation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    private async Task EnsureAppIsLoadedAsync()
    {
        // First check if we were redirected to Authentik and need to log in
        await AuthenticationHelper.LoginIfRequiredAsync(_page);

        // Wait for network to settle
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the sidebar to be visible (indicates app is fully loaded)
        var sidebar = _page.Locator(".app-sidebar");
        await sidebar.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
    }
}

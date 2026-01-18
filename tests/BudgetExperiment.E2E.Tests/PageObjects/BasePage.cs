// <copyright file="BasePage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Base class for all page objects providing common functionality.
/// </summary>
public abstract class BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BasePage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    protected BasePage(IPage page)
    {
        Page = page;
    }

    /// <summary>
    /// Gets the Playwright page instance.
    /// </summary>
    protected IPage Page { get; }

    /// <summary>
    /// Gets the page title locator.
    /// </summary>
    protected ILocator PageTitle => Page.Locator("h1, h2").First;

    /// <summary>
    /// Gets the loading spinner locator (app-specific loading container).
    /// </summary>
    protected ILocator LoadingSpinner => Page.Locator(".loading-container");

    /// <summary>
    /// Gets the Blazor WASM bootstrap loading indicator.
    /// </summary>
    protected ILocator BlazorLoadingIndicator => Page.Locator("#app .loading-progress");

    /// <summary>
    /// Gets the error alert locator.
    /// </summary>
    protected ILocator ErrorAlert => Page.Locator(".alert-danger, .error-alert");

    /// <summary>
    /// Gets the navigation sidebar.
    /// </summary>
    public NavigationComponent Navigation => new(Page);

    /// <summary>
    /// Waits for the page to finish loading (spinner disappears).
    /// </summary>
    /// <param name="timeout">Maximum time to wait in milliseconds.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task WaitForLoadingCompleteAsync(int timeout = 30000)
    {
        // First, wait for Blazor app to bootstrap (loading-progress disappears)
        try
        {
            await BlazorLoadingIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeout,
            });
        }
        catch (TimeoutException)
        {
            // Blazor loading might already be gone, continue
        }

        // Then wait for any app-specific loading spinners to disappear
        var spinnerCount = await LoadingSpinner.CountAsync();
        if (spinnerCount > 0)
        {
            await LoadingSpinner.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeout,
            });
        }

        // Also give the page a moment to stabilize
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Gets the current page title text.
    /// </summary>
    /// <returns>The page title text.</returns>
    public async Task<string> GetPageTitleTextAsync()
    {
        return await PageTitle.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Checks if an error alert is visible.
    /// </summary>
    /// <returns>True if an error is displayed.</returns>
    public async Task<bool> HasErrorAsync()
    {
        return await ErrorAlert.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the error message text if visible.
    /// </summary>
    /// <returns>The error message or null.</returns>
    public async Task<string?> GetErrorMessageAsync()
    {
        if (await HasErrorAsync())
        {
            return await ErrorAlert.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    /// Takes a screenshot of the current page state.
    /// </summary>
    /// <param name="name">Name for the screenshot file.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task TakeScreenshotAsync(string name)
    {
        var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(screenshotsDir, $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png"),
        });
    }
}

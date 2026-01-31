// <copyright file="AuthenticationHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Helper class for authenticating with the demo environment via Authentik.
/// </summary>
public static class AuthenticationHelper
{
    private const string TestUsername = "test";
    private const string TestPassword = "demo123";

    /// <summary>
    /// Logs into the application using test credentials.
    /// If already logged in, this method returns immediately.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <param name="baseUrl">The base URL of the application.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task LoginAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for either: nav menu (already logged in), login button, or authentik redirect
        var navMenu = page.Locator("nav.nav-menu");
        var loginButton = page.GetByRole(AriaRole.Link, new() { Name = "Login" });
        var checkingAuth = page.GetByText("Checking authentication");

        // Authentik uses web components - need multiple selector strategies
        var usernameField = page.Locator("input[name='uidField']")
            .Or(page.Locator("input[type='text'][required]"))
            .Or(page.GetByLabel("Email or Username"));

        // Wait up to 30 seconds for one of these states
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow - startTime < timeout)
        {
            // If still checking authentication, wait
            if (await checkingAuth.IsVisibleAsync())
            {
                await Task.Delay(500);
                continue;
            }

            // Check if already authenticated
            if (await navMenu.IsVisibleAsync())
            {
                // Already logged in, nothing to do
                return;
            }

            // Check if on unauthenticated app page with login button
            if (await loginButton.IsVisibleAsync())
            {
                await loginButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                continue;
            }

            // Check if on Authentik login page
            if (page.Url.Contains("authentik"))
            {
                // Wait for the page to be interactive
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                if (await usernameField.First.IsVisibleAsync())
                {
                    // Perform login
                    await PerformAuthentikLoginAsync(page, baseUrl);
                    return;
                }
            }

            // Wait a bit before checking again
            await Task.Delay(500);
        }

        throw new TimeoutException($"Could not determine authentication state within {timeout.TotalSeconds} seconds. Current URL: {page.Url}");
    }

    /// <summary>
    /// Performs the actual Authentik login flow.
    /// </summary>
    private static async Task PerformAuthentikLoginAsync(IPage page, string baseUrl)
    {
        // Wait for form to be fully loaded and loading overlay to disappear
        await WaitForLoadingOverlayAsync(page);

        // Try multiple selector strategies for the username field
        // Authentik uses web components which may require different selectors
        var usernameField = page.Locator("input[name='uidField']")
            .Or(page.Locator("input[type='text'][required]"))
            .Or(page.GetByLabel("Email or Username"))
            .Or(page.GetByPlaceholder("Email or Username"));

        await usernameField.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await usernameField.First.FillAsync(TestUsername);

        // Click Log in to proceed to password step
        var loginButton = page.GetByRole(AriaRole.Button, new() { Name = "Log in" });
        await loginButton.ClickAsync();

        // Wait for loading overlay to disappear and password field to appear
        await WaitForLoadingOverlayAsync(page);

        var passwordField = page.Locator("input[type='password']")
            .Or(page.GetByLabel("Password"))
            .Or(page.Locator("input[name='password']"));

        await passwordField.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await passwordField.First.FillAsync(TestPassword);

        // Wait for overlay and click Continue/submit button
        await WaitForLoadingOverlayAsync(page);
        var submitButton = page.GetByRole(AriaRole.Button, new() { Name = "Continue" })
            .Or(page.Locator("button[type='submit']"))
            .Or(page.GetByRole(AriaRole.Button, new() { Name = "Log in" }));
        await submitButton.First.ClickAsync(new LocatorClickOptions { Timeout = 10000 });

        // Wait for redirect - poll for either successful redirect or error
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(500);

            // Check if we're back at the app
            if (page.Url.StartsWith(baseUrl) && !page.Url.Contains("authentik"))
            {
                // Wait for navigation to render (indicates app is loaded)
                await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new() { Timeout = 10000 });
                return;
            }

            // Check for permission denied on Authentik
            var permissionDenied = page.GetByText("Permission denied");
            if (await permissionDenied.IsVisibleAsync())
            {
                throw new InvalidOperationException(
                    "Authentik permission denied: The test user does not have access to this application. " +
                    "Please configure Authentik to grant the 'test' user access to the Budget Experiment application.");
            }

            // Check for error messages on Authentik page
            var errorMessage = page.Locator(".pf-c-alert__title, .pf-m-danger, .error-message");
            if (await errorMessage.IsVisibleAsync())
            {
                var errorText = await errorMessage.First.TextContentAsync();
                throw new InvalidOperationException($"Authentik login failed: {errorText}");
            }
        }

        throw new TimeoutException($"Login redirect timed out after {timeout.TotalSeconds} seconds. Current URL: {page.Url}");
    }

    /// <summary>
    /// Waits for the Authentik loading overlay to disappear.
    /// </summary>
    private static async Task WaitForLoadingOverlayAsync(IPage page)
    {
        var loadingOverlay = page.Locator("ak-loading-overlay");
        try
        {
            await loadingOverlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Overlay might not exist, that's fine
        }

        // Also wait for network to settle
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Checks if the current page shows an authenticated state.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <returns>True if the user appears to be logged in.</returns>
    public static async Task<bool> IsLoggedInAsync(IPage page)
    {
        // Check if navigation menu is visible (indicates authenticated state)
        var navMenu = page.Locator("nav.nav-menu");
        try
        {
            await navMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 2000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}

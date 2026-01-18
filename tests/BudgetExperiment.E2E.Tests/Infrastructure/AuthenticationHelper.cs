// <copyright file="AuthenticationHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Infrastructure;

/// <summary>
/// Helper class for handling authentication in E2E tests.
/// </summary>
public static class AuthenticationHelper
{
    /// <summary>
    /// The test username.
    /// </summary>
    public const string TestUsername = "test";

    /// <summary>
    /// The test password.
    /// </summary>
    public const string TestPassword = "demo123";

    /// <summary>
    /// Logs in to the application if redirected to the login page.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task LoginIfRequiredAsync(IPage page)
    {
        // Check if we're on the Authentik login page
        var currentUrl = page.Url;
        if (!currentUrl.Contains("authentik", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            // Wait for the page to fully load
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Wait for the login form to be visible - try various selectors for Authentik
            var usernameField = page.Locator("input[name='uidField']");
            try
            {
                await usernameField.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            }
            catch
            {
                // Authentik may use different field names depending on version
                usernameField = page.Locator("input[autocomplete='username']");
                await usernameField.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            }

            // Fill in username
            await usernameField.FillAsync(TestUsername);

            // Click the login/next button to proceed to password
            // Authentik uses a button with type="submit"
            // Use JavaScript evaluation to click since button may be in fixed position
            var loginButton = page.Locator("button[type='submit']").First;
            await loginButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await loginButton.EvaluateAsync("el => el.click()");

            // Wait for password field - Authentik shows this after username is submitted
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var passwordField = page.Locator("input[type='password']").First;
            await passwordField.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

            // Fill in password
            await passwordField.FillAsync(TestPassword);

            // Click login button again - use JavaScript click
            loginButton = page.Locator("button[type='submit']").First;
            await loginButton.EvaluateAsync("el => el.click()");

            // Wait for redirect back to the app - use a longer timeout and check periodically
            var maxWait = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < maxWait)
            {
                await Task.Delay(500);
                if (!page.Url.Contains("authentik", StringComparison.OrdinalIgnoreCase))
                {
                    // Successfully redirected away from Authentik
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    return;
                }
            }

            throw new Exception($"Login did not redirect away from Authentik within timeout. Current URL: {page.Url}");
        }
        catch (Exception ex)
        {
            // Take a screenshot to help debug
            var screenshotPath = Path.Combine(Path.GetTempPath(), $"auth-failure-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            throw new Exception($"Login failed. Screenshot saved to: {screenshotPath}. Original error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Navigates to a page and handles login if required.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <param name="path">The path to navigate to.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task GoToWithLoginAsync(IPage page, string path)
    {
        await page.GotoAsync(path);

        // Give the page time to redirect if needed
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Handle login if redirected to Authentik
        await LoginIfRequiredAsync(page);

        // Wait for the app to fully load after authentication
        // The app-sidebar indicates the app is authorized and rendered
        var sidebar = page.Locator(".app-sidebar");
        try
        {
            await sidebar.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
        }
        catch
        {
            // If sidebar not found, we might need to login again
            // (could happen if auth state was lost)
            await LoginIfRequiredAsync(page);
            await sidebar.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
        }
    }
}

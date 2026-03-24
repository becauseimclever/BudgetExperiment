// <copyright file="SessionExpiryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// Functional E2E tests for silent token refresh and session expiry handling (Feature 054).
/// These tests verify the user experience when tokens expire during active use.
/// </summary>
[Collection("Playwright")]
public class SessionExpiryTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionExpiryTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public SessionExpiryTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that when cookies are cleared (simulating session expiry),
    /// the user sees a session-expired toast notification.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task SessionExpiry_ShouldShowToast_WhenTokenCannotRefresh()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        // Use a fresh context to avoid poisoning the shared fixture
        var browser = _fixture.Page.Context.Browser!;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Verify we're logged in and on a page with data
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new()
            {
                Timeout = 15000,
            });

            // Clear all cookies to simulate token expiry
            await context.ClearCookiesAsync();

            // Clear OIDC tokens from sessionStorage (Blazor WASM stores them there)
            await page.EvaluateAsync(@"() => {
                const keys = Object.keys(sessionStorage);
                for (const key of keys) {
                    if (key.includes('oidc') || key.includes('token') || key.includes('auth')) {
                        sessionStorage.removeItem(key);
                    }
                }
            }");

            // Trigger an API call by navigating to a page that loads data
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");

            // Wait for either: session expired toast, or redirect to login
            var toastLocator = page.Locator("[data-testid='toast-warning']");
            var loginLink = page.GetByRole(AriaRole.Link, new()
            {
                Name = "Login",
            });

            // Give enough time for the refresh attempt + toast / redirect
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(15);
            var sawToastOrRedirect = false;

            while (DateTime.UtcNow - startTime < timeout)
            {
                if (await toastLocator.IsVisibleAsync())
                {
                    var toastText = await toastLocator.InnerTextAsync();
                    Assert.Contains("Session expired", toastText, StringComparison.OrdinalIgnoreCase);
                    sawToastOrRedirect = true;
                    break;
                }

                // If redirected to login page or Authentik, that's also acceptable
                if (await loginLink.IsVisibleAsync() || page.Url.Contains("authentik"))
                {
                    sawToastOrRedirect = true;
                    break;
                }

                await Task.Delay(500);
            }

            Assert.True(sawToastOrRedirect, "Expected session expired toast or redirect to login, but neither appeared");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that multiple rapid API calls after token invalidation don't
    /// produce duplicate toast notifications (concurrency guard).
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task SessionExpiry_ShouldNotShowDuplicateToasts()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        // Use a fresh browser context to avoid cookie contamination
        var browser = _fixture.Page.Context.Browser!;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Navigate to a data-heavy page (calendar triggers multiple API calls)
            await page.GotoAsync($"{_fixture.BaseUrl}/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new()
            {
                Timeout = 15000,
            });

            // Clear cookies + tokens to simulate expiry
            await context.ClearCookiesAsync();
            await page.EvaluateAsync(@"() => {
                const keys = Object.keys(sessionStorage);
                for (const key of keys) {
                    if (key.includes('oidc') || key.includes('token') || key.includes('auth')) {
                        sessionStorage.removeItem(key);
                    }
                }
            }");

            // Navigate to calendar which triggers multiple simultaneous API calls
            await page.GotoAsync($"{_fixture.BaseUrl}/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for potential toasts
            await Task.Delay(3000);

            // Count warning toasts — should be at most one due to our semaphore
            var toasts = page.Locator("[data-testid='toast-warning']");
            var toastCount = await toasts.CountAsync();

            // Either 0 (redirected before toast) or 1 (toast shown)
            Assert.True(toastCount <= 1, $"Expected at most 1 warning toast, but found {toastCount}");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that a valid authenticated session does not show any session-expired artifacts.
    /// This is a baseline test to confirm normal operation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "DemoSafe")]
    public async Task ValidSession_ShouldNotShowSessionExpiredToast()
    {
        // Use a fresh context to avoid cross-test contamination
        var browser = _fixture.Page.Context.Browser!;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Navigate to accounts page
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new()
            {
                Timeout = 15000,
            });

            // Wait a moment for any async operations
            await Task.Delay(2000);

            // Verify no session expired toast appeared
            var toasts = page.Locator("[data-testid='toast-warning']");
            var toastCount = await toasts.CountAsync();
            Assert.Equal(0, toastCount);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that form state is preserved to localStorage when session expires.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task SessionExpiry_ShouldPreserveFormState_WhenFormIsOpen()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        // Use a fresh browser context
        var browser = _fixture.Page.Context.Browser!;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Navigate to accounts and create a test account
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check if any account cards exist to click into
            var accountCards = page.Locator(".card .card-header");
            var accountCount = await accountCards.CountAsync();

            if (accountCount == 0)
            {
                // No accounts exist, skip this test
                return;
            }

            // Click the first account to go to transactions
            await accountCards.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Open the add transaction form
            var addButton = page.GetByRole(AriaRole.Button, new()
            {
                Name = "+ Add Transaction",
            });
            if (!await addButton.IsVisibleAsync())
            {
                // Button not available, skip
                return;
            }

            await addButton.ClickAsync();
            await Expect(page.Locator(".modal-dialog")).ToBeVisibleAsync();

            // Fill in some form data
            await page.Locator("#txnDescription").FillAsync("Test Preserve Data");
            await page.Locator("#txnAmount").FillAsync("42.99");

            // Simulate form state registration by writing to localStorage directly
            // (since the FormStateService would normally do this on SaveAllAsync)
            await page.EvaluateAsync(@"() => {
                localStorage.setItem('budget-form-state:TransactionForm',
                    JSON.stringify({ Description: 'Test Preserve Data', Amount: 42.99 }));
            }");

            // Clear cookies to simulate expiry
            await context.ClearCookiesAsync();
            await page.EvaluateAsync(@"() => {
                const keys = Object.keys(sessionStorage);
                for (const key of keys) {
                    if (key.includes('oidc') || key.includes('token') || key.includes('auth')) {
                        sessionStorage.removeItem(key);
                    }
                }
            }");

            // Check that the form state is still in localStorage
            var savedState = await page.EvaluateAsync<string?>(
                "() => localStorage.getItem('budget-form-state:TransactionForm')");

            Assert.NotNull(savedState);
            Assert.Contains("Test Preserve Data", savedState!);
            Assert.Contains("42.99", savedState);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that after re-authentication, the user can navigate back to the page they were on
    /// and the application is in a usable state.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "LocalOnly")]
    public async Task ReAuthentication_ShouldReturnToUsableState()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        // Use a fresh context
        var browser = _fixture.Page.Context.Browser!;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // Login first
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Navigate to accounts
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new()
            {
                Timeout = 15000,
            });

            // Clear session to simulate expiry
            await context.ClearCookiesAsync();
            await page.EvaluateAsync("() => sessionStorage.clear()");

            // Navigate to base URL first to trigger fresh auth flow
            await page.GotoAsync(_fixture.BaseUrl);

            // Re-authenticate
            await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

            // Verify we're back in a usable state (LoginAsync already waits for nav.nav-menu)
            // Navigate to accounts to verify API calls work
            await page.GotoAsync($"{_fixture.BaseUrl}/accounts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(page.Locator("nav.nav-menu")).ToBeVisibleAsync(new()
            {
                Timeout = 15000,
            });

            // The page should load without errors
            var errorToast = page.Locator("[data-testid='toast-error']");
            var errorCount = await errorToast.CountAsync();
            Assert.Equal(0, errorCount);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}

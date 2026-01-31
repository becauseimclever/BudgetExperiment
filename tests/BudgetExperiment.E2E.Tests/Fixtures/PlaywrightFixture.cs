// <copyright file="PlaywrightFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Fixtures;

/// <summary>
/// Playwright test fixture that manages browser lifecycle and provides environment configuration.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? browser;

    /// <summary>
    /// Gets the browser context for the current test.
    /// </summary>
    public IBrowserContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the page for the current test.
    /// </summary>
    public IPage Page { get; private set; } = null!;

    /// <summary>
    /// Gets the base URL for the application under test.
    /// </summary>
    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("BUDGET_APP_URL")
        ?? "https://budgetdemo.becauseimclever.com";

    /// <summary>
    /// Gets a value indicating whether the browser should run in headed mode.
    /// </summary>
    public bool Headed { get; } = Environment.GetEnvironmentVariable("HEADED")
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Gets the slow motion delay in milliseconds for debugging.
    /// </summary>
    public float SlowMo { get; } = float.TryParse(
        Environment.GetEnvironmentVariable("SLOWMO"),
        out var slowMo) ? slowMo : 0;

    /// <summary>
    /// Gets a value indicating whether to capture traces for debugging.
    /// </summary>
    public bool CaptureTraces { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACES")
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !Headed,
            SlowMo = SlowMo,
        });

        Context = await browser.NewContextAsync();

        if (CaptureTraces)
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        Page = await Context.NewPageAsync();

        // Verify server is reachable
        await ValidateServerAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (CaptureTraces)
        {
            var tracePath = Path.Combine("playwright-traces", $"trace-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
            Directory.CreateDirectory("playwright-traces");
            await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        }

        await Context.DisposeAsync();
        await browser!.DisposeAsync();
        playwright!.Dispose();
    }

    private async Task ValidateServerAsync()
    {
        var response = await Page.GotoAsync(BaseUrl);
        if (response?.Status >= 400)
        {
            throw new InvalidOperationException($"Server at {BaseUrl} returned {response.Status}");
        }
    }
}

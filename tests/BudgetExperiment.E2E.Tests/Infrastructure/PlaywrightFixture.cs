// <copyright file="PlaywrightFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;

namespace BudgetExperiment.E2E.Tests.Infrastructure;

/// <summary>
/// Provides Playwright browser instance for E2E tests.
/// Implements IAsyncLifetime for proper async setup/teardown.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private Process? _serverProcess;
    private bool _serverStartedByFixture;

    /// <summary>
    /// Gets the base URL for the application under test.
    /// Defaults to http://localhost:5099, can be overridden via BUDGET_APP_URL environment variable.
    /// </summary>
    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("BUDGET_APP_URL") ?? "http://localhost:5099";

    /// <summary>
    /// Gets the Playwright instance.
    /// </summary>
    public IPlaywright Playwright { get; private set; } = null!;

    /// <summary>
    /// Gets the browser instance.
    /// </summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether to run in headed mode (visible browser).
    /// Set HEADED=true environment variable to enable.
    /// </summary>
    public bool Headed { get; } = Environment.GetEnvironmentVariable("HEADED")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Initializes Playwright and launches the browser.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        // Ensure the server is running (start it if needed)
        await EnsureServerIsRunningAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !Headed,
            SlowMo = Headed ? 100 : 0, // Slow down when watching
        });
    }

    /// <summary>
    /// Creates a new browser page for a test.
    /// </summary>
    /// <returns>A new page instance.</returns>
    public async Task<IPage> CreatePageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
        });

        return await context.NewPageAsync();
    }

    /// <summary>
    /// Cleans up browser and Playwright resources.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();

        // Stop the server if we started it
        if (_serverStartedByFixture && _serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(entireProcessTree: true);
            _serverProcess.Dispose();
        }
    }

    private async Task EnsureServerIsRunningAsync()
    {
        // First, check if server is already running
        if (await IsServerRunningAsync())
        {
            return;
        }

        // Start the server
        await StartServerAsync();

        // Wait for server to be ready
        await WaitForServerReadyAsync();
    }

    private async Task<bool> IsServerRunningAsync()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private Task StartServerAsync()
    {
        // Find the solution root (go up from test project bin folder)
        var currentDir = AppContext.BaseDirectory;
        var solutionDir = FindSolutionDirectory(currentDir);

        if (solutionDir == null)
        {
            throw new InvalidOperationException(
                "Cannot find solution directory. " +
                "Ensure the API server is running: dotnet run --project src/BudgetExperiment.Api");
        }

        var apiProjectPath = Path.Combine(solutionDir, "src", "BudgetExperiment.Api", "BudgetExperiment.Api.csproj");

        if (!File.Exists(apiProjectPath))
        {
            throw new InvalidOperationException(
                $"Cannot find API project at {apiProjectPath}. " +
                "Ensure the API server is running: dotnet run --project src/BudgetExperiment.Api");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{apiProjectPath}\" --no-build",
            WorkingDirectory = solutionDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _serverProcess = Process.Start(startInfo);
        _serverStartedByFixture = true;

        if (_serverProcess == null)
        {
            throw new InvalidOperationException("Failed to start the API server process.");
        }

        return Task.CompletedTask;
    }

    private async Task WaitForServerReadyAsync()
    {
        const int maxWaitSeconds = 60;
        const int checkIntervalMs = 500;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < maxWaitSeconds)
        {
            if (await IsServerRunningAsync())
            {
                return;
            }

            // Check if process died
            if (_serverProcess != null && _serverProcess.HasExited)
            {
                var exitCode = _serverProcess.ExitCode;
                throw new InvalidOperationException(
                    $"API server process exited unexpectedly with code {exitCode}. " +
                    "Check that the API project builds and runs correctly.");
            }

            await Task.Delay(checkIntervalMs);
        }

        throw new TimeoutException(
            $"API server did not become ready within {maxWaitSeconds} seconds. " +
            "Ensure the API server can start successfully: dotnet run --project src/BudgetExperiment.Api");
    }

    private static string? FindSolutionDirectory(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "BudgetExperiment.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

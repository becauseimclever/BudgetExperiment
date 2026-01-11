// -----------------------------------------------------------------------
// <copyright file="ThemeService.cs" company="Budget Experiment">
//     Copyright (c) Budget Experiment. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

/// <summary>
/// Service for managing application theme (light, dark, vscode-dark, system).
/// </summary>
public sealed class ThemeService : IAsyncDisposable
{
    private const string StorageKey = "budget-experiment-theme";
    private const string DefaultTheme = "system";

    private readonly IJSRuntime jsRuntime;
    private IJSObjectReference? module;
    private string currentTheme = DefaultTheme;
    private bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public ThemeService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event Action<string>? ThemeChanged;

    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentTheme => this.currentTheme;

    /// <summary>
    /// Gets the available theme options.
    /// </summary>
    public static IReadOnlyList<ThemeOption> AvailableThemes { get; } = new List<ThemeOption>
    {
        new("system", "System", "monitor"),
        new("light", "Light", "sun"),
        new("dark", "Dark", "moon"),
        new("vscode-dark", "VS Code", "code"),
    };

    /// <summary>
    /// Initializes the theme service by loading the saved theme from localStorage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        if (this.isInitialized)
        {
            return;
        }

        try
        {
            this.module = await this.jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/theme.js");

            var savedTheme = await this.module.InvokeAsync<string?>("getTheme");
            this.currentTheme = savedTheme ?? DefaultTheme;

            await this.ApplyThemeAsync(this.currentTheme);
            this.isInitialized = true;
        }
        catch (JSException)
        {
            // JS interop not available (e.g., prerendering)
            this.currentTheme = DefaultTheme;
        }
    }

    /// <summary>
    /// Sets the current theme.
    /// </summary>
    /// <param name="theme">The theme name (light, dark, vscode-dark, system).</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetThemeAsync(string theme)
    {
        if (string.IsNullOrEmpty(theme))
        {
            theme = DefaultTheme;
        }

        this.currentTheme = theme;

        try
        {
            if (this.module != null)
            {
                await this.module.InvokeVoidAsync("setTheme", theme);
            }

            this.ThemeChanged?.Invoke(theme);
        }
        catch (JSException)
        {
            // Ignore JS errors
        }
    }

    /// <summary>
    /// Gets the resolved theme (resolves 'system' to actual light/dark).
    /// </summary>
    /// <returns>The resolved theme name.</returns>
    public async Task<string> GetResolvedThemeAsync()
    {
        if (this.module == null)
        {
            return "light";
        }

        try
        {
            return await this.module.InvokeAsync<string>("getResolvedTheme");
        }
        catch (JSException)
        {
            return "light";
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (this.module != null)
        {
            try
            {
                await this.module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
        }
    }

    private async Task ApplyThemeAsync(string theme)
    {
        if (this.module != null)
        {
            await this.module.InvokeVoidAsync("applyTheme", theme);
        }
    }
}

/// <summary>
/// Represents a theme option for display.
/// </summary>
/// <param name="Value">The theme value.</param>
/// <param name="Label">The display label.</param>
/// <param name="Icon">The icon name.</param>
public sealed record ThemeOption(string Value, string Label, string Icon);

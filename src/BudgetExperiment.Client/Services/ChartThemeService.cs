// <copyright file="ChartThemeService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Provides ApexCharts theme options derived from the active BudgetExperiment theme.
/// Hardcoded colour values mirror the CSS design tokens defined in
/// <c>wwwroot/css/design-system/tokens.css</c> and the per-theme overrides in
/// <c>wwwroot/css/themes/</c>.
/// </summary>
public sealed class ChartThemeService : IChartThemeService
{
    // Themes that render with a dark background — all others are treated as light.
    private static readonly HashSet<string> DarkThemeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "dark",
        "vscode-dark",
    };

    // Light-mode colour tokens (tokens.css defaults).
    private static readonly string[] LightSeriesColors =
    [
        "#107c10", // income green  (--color-income)
        "#d13438", // expense red   (--color-expense)
        "#0078d4", // transfer blue (--color-transfer)
        "#8764b8", // recurring purple (--color-recurring)
        "#ffb900", // amber         (--color-warning)
        "#038387", // teal
        "#05a6f0", // sky blue
        "#498205", // lime
    ];

    // Dark-mode colour tokens (dark.css overrides).
    private static readonly string[] DarkSeriesColors =
    [
        "#6ccb5f", // income green  (dark --color-income)
        "#f87c7c", // expense red   (dark --color-expense)
        "#4ba0e8", // transfer blue (dark --color-transfer)
        "#b794d4", // recurring purple (dark --color-recurring)
        "#ffc83d", // amber         (dark --color-warning)
        "#4cbaba", // teal
        "#50b7e1", // sky blue
        "#78c22a", // lime
    ];

    private readonly ThemeService _themeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartThemeService"/> class.
    /// </summary>
    /// <param name="themeService">The theme service used to retrieve the active theme name.</param>
    public ChartThemeService(ThemeService themeService)
    {
        _themeService = themeService;
    }

    private bool IsDark => DarkThemeNames.Contains(_themeService.CurrentTheme);

    /// <inheritdoc/>
    public string GetApexChartsThemeMode() => IsDark ? "dark" : "light";

    /// <inheritdoc/>
    public string GetChartBackground() => IsDark ? "#292929" : "#ffffff";

    /// <inheritdoc/>
    public string[] GetSeriesColors() => IsDark ? DarkSeriesColors : LightSeriesColors;

    /// <inheritdoc/>
    public string GetGridColor() => IsDark ? "#484644" : "#d2d0ce";

    /// <inheritdoc/>
    public string GetLabelColor() => IsDark ? "#b3b0ad" : "#605e5c";
}

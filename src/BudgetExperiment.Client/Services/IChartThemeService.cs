// <copyright file="IChartThemeService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Bridges the CSS custom property theming system to ApexCharts configuration.
/// Because CSS custom property values cannot be read from C#, this service
/// derives chart options from the active theme name and returns hardcoded
/// colour values that are consistent with each theme's design tokens.
/// </summary>
public interface IChartThemeService
{
    /// <summary>
    /// Returns a theme mode string compatible with ApexCharts ("light" or "dark"),
    /// derived from the currently active BudgetExperiment theme.
    /// </summary>
    /// <returns>"dark" for dark-mode themes; "light" for all others.</returns>
    string GetApexChartsThemeMode();

    /// <summary>
    /// Returns the background colour for charts, matching the active theme's
    /// surface colour (CSS <c>--color-surface</c>).
    /// </summary>
    /// <returns>A hex colour string, e.g. "#ffffff".</returns>
    string GetChartBackground();

    /// <summary>
    /// Returns an ordered array of colours for chart series, matching the active
    /// theme's design tokens. Contains at least eight colours.
    /// </summary>
    /// <returns>An array of hex colour strings.</returns>
    string[] GetSeriesColors();

    /// <summary>
    /// Returns the grid/axis line colour for charts, matching the active theme's
    /// border colour (CSS <c>--color-border</c>).
    /// </summary>
    /// <returns>A hex colour string.</returns>
    string GetGridColor();

    /// <summary>
    /// Returns the axis label text colour for charts, matching the active theme's
    /// secondary text colour (CSS <c>--color-text-secondary</c>).
    /// </summary>
    /// <returns>A hex colour string.</returns>
    string GetLabelColor();
}

// <copyright file="ChartThemeServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Threading;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChartThemeService"/>.
/// </summary>
public sealed class ChartThemeServiceTests
{
    /// <summary>
    /// Verifies that a "dark" theme maps to ApexCharts "dark" mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetApexChartsThemeMode_DarkTheme_ReturnsDark()
    {
        var sut = await CreateSutAsync("dark");

        Assert.Equal("dark", sut.GetApexChartsThemeMode());
    }

    /// <summary>
    /// Verifies that a "light" theme maps to ApexCharts "light" mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetApexChartsThemeMode_LightTheme_ReturnsLight()
    {
        var sut = await CreateSutAsync("light");

        Assert.Equal("light", sut.GetApexChartsThemeMode());
    }

    /// <summary>
    /// Verifies that a "vscode-dark" theme maps to ApexCharts "dark" mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetApexChartsThemeMode_VsCodeDarkTheme_ReturnsDark()
    {
        var sut = await CreateSutAsync("vscode-dark");

        Assert.Equal("dark", sut.GetApexChartsThemeMode());
    }

    /// <summary>
    /// Verifies that <see cref="IChartThemeService.GetSeriesColors"/> returns at least eight colours.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetSeriesColors_ReturnsAtLeastEightColors()
    {
        var sut = await CreateSutAsync("light");

        Assert.True(sut.GetSeriesColors().Length >= 8);
    }

    /// <summary>
    /// Verifies that <see cref="IChartThemeService.GetGridColor"/> returns a non-empty string.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetGridColor_ReturnsNonEmptyString()
    {
        var sut = await CreateSutAsync("light");

        Assert.False(string.IsNullOrEmpty(sut.GetGridColor()));
    }

    /// <summary>
    /// Verifies that <see cref="IChartThemeService.GetLabelColor"/> returns a non-empty string.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GetLabelColor_ReturnsNonEmptyString()
    {
        var sut = await CreateSutAsync("light");

        Assert.False(string.IsNullOrEmpty(sut.GetLabelColor()));
    }

    private static async Task<IChartThemeService> CreateSutAsync(string theme)
    {
        var themeService = new ThemeService(new StubJSRuntime());
        await themeService.SetThemeAsync(theme);
        return new ChartThemeService(themeService);
    }

    /// <summary>
    /// Stub JavaScript runtime that returns default values without actual JS interop.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}

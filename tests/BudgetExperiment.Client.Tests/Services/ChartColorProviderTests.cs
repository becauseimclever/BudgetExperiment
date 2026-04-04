// <copyright file="ChartColorProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChartColorProvider"/>.
/// </summary>
public sealed class ChartColorProviderTests
{
    private readonly IChartColorProvider _sut = new ChartColorProvider();

    /// <summary>
    /// Verifies that <see cref="IChartColorProvider.GetIncomeColor"/> returns a hex colour string.
    /// </summary>
    [Fact]
    public void GetIncomeColor_ReturnsGreenish()
    {
        var color = _sut.GetIncomeColor();

        Assert.StartsWith("#", color);
        Assert.False(string.IsNullOrEmpty(color));
    }

    /// <summary>
    /// Verifies that <see cref="IChartColorProvider.GetExpenseColor"/> returns a hex colour string.
    /// </summary>
    [Fact]
    public void GetExpenseColor_ReturnsReddish()
    {
        var color = _sut.GetExpenseColor();

        Assert.StartsWith("#", color);
        Assert.False(string.IsNullOrEmpty(color));
    }

    /// <summary>
    /// Verifies that <see cref="IChartColorProvider.GetTransferColor"/> returns a hex colour string.
    /// </summary>
    [Fact]
    public void GetTransferColor_ReturnsHexColor()
    {
        var color = _sut.GetTransferColor();

        Assert.StartsWith("#", color);
        Assert.False(string.IsNullOrEmpty(color));
    }

    /// <summary>
    /// Verifies that <see cref="IChartColorProvider.GetDefaultPalette"/> returns at least eight distinct colours.
    /// </summary>
    [Fact]
    public void GetDefaultPalette_ReturnsAtLeastEightColors()
    {
        var palette = _sut.GetDefaultPalette();

        Assert.True(palette.Length >= 8);
        Assert.Equal(palette.Length, palette.Distinct().Count());
    }

    /// <summary>
    /// Verifies that <see cref="IChartColorProvider.GetCategoryColor"/> always returns a non-empty
    /// string for an unknown category name, falling back to the default palette.
    /// </summary>
    [Fact]
    public void GetCategoryColor_UnknownCategory_ReturnsFallbackPaletteColor()
    {
        var color = _sut.GetCategoryColor("some-unknown-category-xyz");

        Assert.False(string.IsNullOrEmpty(color));
        Assert.StartsWith("#", color);
    }
}

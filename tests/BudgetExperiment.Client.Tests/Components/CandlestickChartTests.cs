// <copyright file="CandlestickChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.CandlestickChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the CandlestickChart component.
/// </summary>
public class CandlestickChartTests : BunitContext
{
    private static readonly DateOnly _jan = new DateOnly(2026, 1, 1);

    [Fact]
    public void CandlestickChart_Renders_EmptyState_WhenNoCandles()
    {
        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, new List<CandlestickDataPoint>()));

        // Assert
        var empty = cut.Find(".candlestick-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void CandlestickChart_Renders_SVG_WhenCandlesProvided()
    {
        // Arrange — one bullish candle
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, Open: 1000m, High: 1200m, Low: 950m, Close: 1100m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert
        var svg = cut.Find("svg.candlestick-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void CandlestickChart_Renders_OneBodyPerCandle()
    {
        // Arrange — 3 candles of mixed direction
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, 1000m, 1200m, 950m, 1100m),
            new(_jan.AddMonths(1), 1100m, 1300m, 1050m, 1250m),
            new(_jan.AddMonths(2), 1250m, 1280m, 1050m, 1080m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert
        var bodies = cut.FindAll("rect.candlestick-body");
        Assert.Equal(3, bodies.Count);
    }

    [Fact]
    public void CandlestickChart_Renders_WicksPerCandle()
    {
        // Arrange — one candle → at least one wick line
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, 1000m, 1200m, 950m, 1100m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert — component must render at least one wick per candle
        var wicks = cut.FindAll("line.candlestick-wick");
        Assert.NotEmpty(wicks);
    }

    [Fact]
    public void CandlestickChart_BullishCandle_HasBullishClass()
    {
        // Arrange — Close (1100) > Open (1000) → IsBullish = true
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, Open: 1000m, High: 1200m, Low: 950m, Close: 1100m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert
        var body = cut.Find("rect.candlestick-body");
        Assert.Contains("candlestick-bullish", body.ClassName ?? string.Empty);
    }

    [Fact]
    public void CandlestickChart_BearishCandle_HasBearishClass()
    {
        // Arrange — Close (1080) < Open (1250) → IsBullish = false
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, Open: 1250m, High: 1280m, Low: 1050m, Close: 1080m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert
        var body = cut.Find("rect.candlestick-body");
        Assert.Contains("candlestick-bearish", body.ClassName ?? string.Empty);
    }

    [Fact]
    public void CandlestickChart_UsesAriaLabel()
    {
        // Arrange
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, 1000m, 1200m, 950m, 1100m),
        };

        const string label = "2026 Q1 balance candlestick chart";

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles)
            .Add(p => p.AriaLabel, label));

        // Assert
        var container = cut.Find(".candlestick-chart");
        Assert.Equal(label, container.GetAttribute("aria-label"));
    }

    [Fact]
    public void CandlestickChart_DojiBullish_HasBullishClass()
    {
        // Arrange — doji candle: Close == Open → IsBullish = true (tie goes to bullish)
        var candles = new List<CandlestickDataPoint>
        {
            new(_jan, Open: 1000m, High: 1050m, Low: 980m, Close: 1000m),
        };

        // Act
        var cut = Render<CandlestickChart>(parameters => parameters
            .Add(p => p.Candles, candles));

        // Assert — doji is treated as bullish (Close >= Open)
        var body = cut.Find("rect.candlestick-body");
        Assert.Contains("candlestick-bullish", body.ClassName ?? string.Empty);
    }
}

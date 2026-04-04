// <copyright file="CandlestickChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG candlestick chart for visualizing account balance OHLC data over time intervals.
/// </summary>
public partial class CandlestickChart
{
    /// <summary>
    /// Gets or sets the list of candles to render.
    /// </summary>
    [Parameter]
    public IReadOnlyList<CandlestickDataPoint>? Candles { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Account balance candlestick chart";

    private static double ViewBoxWidth => 400;

    private static double ViewBoxHeight => 200;

    private static double MarginLeft => 45;

    private static double MarginRight => 10;

    private static double MarginTop => 10;

    private static double MarginBottom => 24;

    private static double ChartWidth => ViewBoxWidth - MarginLeft - MarginRight;

    private static double ChartHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private static double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private static double PlotRight => ViewBoxWidth - MarginRight;

    private bool IsEmpty => Candles == null || Candles.Count == 0;

    private static string GetCandleBodyClass(CandleRenderInfo candle)
    {
        return candle.IsBullish
            ? "candlestick-body candlestick-bullish"
            : "candlestick-body candlestick-bearish";
    }

    private static double MapY(double value, double minVal, double range)
    {
        var ratio = range > 0 ? (value - minVal) / range : 0.5;
        return ChartAreaBottom - (ratio * ChartHeight);
    }

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private IReadOnlyList<CandleRenderInfo> ComputeCandleRenders()
    {
        if (Candles == null || Candles.Count == 0)
        {
            return [];
        }

        var count = Candles.Count;
        var maxVal = Candles.Max(c => (double)c.High);
        var minVal = Candles.Min(c => (double)c.Low);
        var range = maxVal - minVal;
        var slotWidth = ChartWidth / count;
        var bodyWidth = slotWidth / 1.5;
        var infos = new List<CandleRenderInfo>(count);

        for (var i = 0; i < count; i++)
        {
            var c = Candles[i];
            var centerX = MarginLeft + ((i + 0.5) * slotWidth);
            var highY = MapY((double)c.High, minVal, range);
            var lowY = MapY((double)c.Low, minVal, range);
            var openY = MapY((double)c.Open, minVal, range);
            var closeY = MapY((double)c.Close, minVal, range);

            var bodyTop = Math.Min(openY, closeY);
            var bodyBottom = Math.Max(openY, closeY);
            var bodyHeight = Math.Max(1d, bodyBottom - bodyTop);

            infos.Add(new CandleRenderInfo(
                CenterX: centerX,
                WickTop: highY,
                WickBottom: lowY,
                BodyX: centerX - (bodyWidth / 2),
                BodyY: bodyTop,
                BodyWidth: bodyWidth,
                BodyHeight: bodyHeight,
                IsBullish: c.IsBullish));
        }

        return infos;
    }

    private sealed record CandleRenderInfo(
        double CenterX,
        double WickTop,
        double WickBottom,
        double BodyX,
        double BodyY,
        double BodyWidth,
        double BodyHeight,
        bool IsBullish);
}

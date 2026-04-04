// <copyright file="CandlestickInterval.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Controls the grouping interval for candlestick data points.
/// </summary>
public enum CandlestickInterval
{
    /// <summary>One candle per calendar month.</summary>
    Monthly = 0,

    /// <summary>One candle per calendar week.</summary>
    Weekly,
}

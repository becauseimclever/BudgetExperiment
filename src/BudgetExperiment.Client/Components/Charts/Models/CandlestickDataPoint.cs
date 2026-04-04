// <copyright file="CandlestickDataPoint.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents one candle (one interval of account balance data) in a candlestick chart.
/// </summary>
/// <param name="Date">The first day of the interval (e.g. the first of the month).</param>
/// <param name="Open">The opening balance at the start of the interval.</param>
/// <param name="High">The highest balance recorded during the interval.</param>
/// <param name="Low">The lowest balance recorded during the interval.</param>
/// <param name="Close">The closing balance at the end of the interval.</param>
public sealed record CandlestickDataPoint(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close)
{
    /// <summary>
    /// Gets a value indicating whether the closing balance is greater than or equal to the opening balance.
    /// </summary>
    public bool IsBullish => Close >= Open;
}

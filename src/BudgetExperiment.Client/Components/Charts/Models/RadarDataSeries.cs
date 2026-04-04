// <copyright file="RadarDataSeries.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single named series in a radar (spider) chart, with one value per category axis.
/// </summary>
/// <param name="Label">The series name displayed in the legend.</param>
/// <param name="Values">Numeric values for each category axis, in the same order as the chart's Categories list.</param>
public sealed record RadarDataSeries(string Label, IReadOnlyList<decimal> Values);

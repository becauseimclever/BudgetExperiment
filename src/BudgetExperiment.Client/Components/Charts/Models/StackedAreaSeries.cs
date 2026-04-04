// <copyright file="StackedAreaSeries.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single named series rendered as a filled area in a stacked area chart.
/// </summary>
/// <param name="Label">The display label for this series.</param>
/// <param name="Color">The fill color (CSS color string) for this series.</param>
/// <param name="Points">The ordered list of data points that make up this series.</param>
public sealed record StackedAreaSeries(string Label, string Color, IReadOnlyList<StackedAreaDataPoint> Points);

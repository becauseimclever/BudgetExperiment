// <copyright file="StackedAreaDataPoint.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single data point (date + value) within one series of a stacked area chart.
/// </summary>
/// <param name="Date">The date for this data point.</param>
/// <param name="Value">The value at this date.</param>
public sealed record StackedAreaDataPoint(DateOnly Date, decimal Value);

// <copyright file="HeatmapDataPoint.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single cell in a spending heatmap grid.
/// </summary>
/// <param name="DayOfWeek">The day of the week (0 = Monday, 6 = Sunday).</param>
/// <param name="WeekIndex">The zero-based column index of the week.</param>
/// <param name="TotalAmount">The total spending amount for this cell.</param>
/// <param name="TransactionCount">The number of transactions in this cell.</param>
public sealed record HeatmapDataPoint(int DayOfWeek, int WeekIndex, decimal TotalAmount, int TransactionCount);

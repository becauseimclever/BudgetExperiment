// <copyright file="ScatterDataPoint.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single data point in a transaction scatter plot.
/// </summary>
/// <param name="Date">The date of the transaction.</param>
/// <param name="Amount">The transaction amount.</param>
/// <param name="Category">The transaction category name.</param>
/// <param name="IsOutlier">Whether this point is classified as a statistical outlier.</param>
public sealed record ScatterDataPoint(DateOnly Date, decimal Amount, string Category, bool IsOutlier = false);

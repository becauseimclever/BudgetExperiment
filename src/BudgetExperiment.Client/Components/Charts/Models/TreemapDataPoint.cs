// <copyright file="TreemapDataPoint.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents a single data point in a treemap chart, defining a labelled segment with an optional colour override.
/// </summary>
/// <param name="Label">The display label for this segment.</param>
/// <param name="Value">The numeric value determining the segment's relative size.</param>
/// <param name="Color">Optional hex colour override for this segment (e.g. "#4e79a7").</param>
public sealed record TreemapDataPoint(string Label, decimal Value, string? Color = null);

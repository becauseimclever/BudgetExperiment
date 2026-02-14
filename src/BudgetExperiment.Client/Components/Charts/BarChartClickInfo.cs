// <copyright file="BarChartClickInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Represents a bar click payload.
/// </summary>
public sealed record BarChartClickInfo(string GroupLabel, string SeriesName, decimal Value);

// <copyright file="KakeiboGroupedSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for Kakeibo-grouped spending summary totals.
/// </summary>
public sealed class KakeiboGroupedSummaryDto
{
    /// <summary>Gets or sets the total spent on essentials.</summary>
    public decimal Essentials { get; set; }

    /// <summary>Gets or sets the total spent on wants.</summary>
    public decimal Wants { get; set; }

    /// <summary>Gets or sets the total spent on culture.</summary>
    public decimal Culture { get; set; }

    /// <summary>Gets or sets the total spent on unexpected expenses.</summary>
    public decimal Unexpected { get; set; }

    /// <summary>Gets or sets the total across all Kakeibo categories.</summary>
    public decimal Total { get; set; }
}

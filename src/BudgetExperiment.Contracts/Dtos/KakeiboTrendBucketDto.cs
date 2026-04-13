// <copyright file="KakeiboTrendBucketDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// A single spending bucket entry in a monthly Kakeibo trend breakdown.
/// </summary>
public sealed class KakeiboTrendBucketDto
{
    /// <summary>Gets or sets the Kakeibo category label (e.g. "Essentials", "Wants", "Culture", "Unexpected"), if applicable.</summary>
    public string? KakeiboCategory
    {
        get; set;
    }

    /// <summary>Gets or sets the budget category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the total amount spent in this bucket for the month.</summary>
    public MoneyDto Amount { get; set; } = new();
}

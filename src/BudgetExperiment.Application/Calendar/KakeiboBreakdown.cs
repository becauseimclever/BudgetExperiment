// <copyright file="KakeiboBreakdown.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Application-layer Kakeibo spending breakdown for a period.
/// </summary>
public sealed class KakeiboBreakdown
{
    /// <summary>Gets or sets the total spent on essential needs.</summary>
    public decimal EssentialsAmount { get; set; }

    /// <summary>Gets or sets the total spent on wants.</summary>
    public decimal WantsAmount { get; set; }

    /// <summary>Gets or sets the total spent on culture.</summary>
    public decimal CultureAmount { get; set; }

    /// <summary>Gets or sets the total spent on unexpected expenses.</summary>
    public decimal UnexpectedAmount { get; set; }

    /// <summary>Gets the total of all Kakeibo categories.</summary>
    public decimal TotalSpend => EssentialsAmount + WantsAmount + CultureAmount + UnexpectedAmount;
}

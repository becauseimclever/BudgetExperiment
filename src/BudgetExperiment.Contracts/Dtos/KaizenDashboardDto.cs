// <copyright file="KaizenDashboardDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// The 12-week rolling Kaizen dashboard report combining Kakeibo weekly spending
/// aggregations with Kaizen micro-goal outcomes.
/// </summary>
public sealed class KaizenDashboardDto
{
    /// <summary>
    /// Gets or sets the ordered list of weekly Kakeibo summaries (oldest first).
    /// </summary>
    public IReadOnlyList<WeeklyKakeiboSummaryDto> Weeks { get; set; } = Array.Empty<WeeklyKakeiboSummaryDto>();
}

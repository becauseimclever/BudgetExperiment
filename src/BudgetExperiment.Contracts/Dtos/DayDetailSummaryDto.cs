// <copyright file="DayDetailSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for day detail summary with pre-computed totals.
/// </summary>
public sealed class DayDetailSummaryDto
{
    /// <summary>Gets or sets the total of actual transactions.</summary>
    public MoneyDto TotalActual { get; set; } = new();

    /// <summary>Gets or sets the total of projected recurring transactions.</summary>
    public MoneyDto TotalProjected { get; set; } = new();

    /// <summary>Gets or sets the combined total.</summary>
    public MoneyDto CombinedTotal { get; set; } = new();

    /// <summary>Gets or sets the total item count.</summary>
    public int ItemCount { get; set; }
}

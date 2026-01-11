// <copyright file="DayDetailDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for detailed day view with all transactions and recurring instances.
/// </summary>
public sealed class DayDetailDto
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the list of items (transactions and recurring instances).</summary>
    public IReadOnlyList<DayDetailItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the summary for this day.</summary>
    public DayDetailSummaryDto Summary { get; set; } = new();
}

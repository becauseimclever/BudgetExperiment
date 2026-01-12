// <copyright file="PastDueSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO containing a summary of past-due recurring items.
/// </summary>
public sealed class PastDueSummaryDto
{
    /// <summary>Gets or sets the list of past-due items.</summary>
    public IReadOnlyList<PastDueItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total count of past-due items.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the oldest past-due date.</summary>
    public DateOnly? OldestDate { get; set; }

    /// <summary>Gets or sets the total amount of all past-due items.</summary>
    public MoneyDto? TotalAmount { get; set; }
}

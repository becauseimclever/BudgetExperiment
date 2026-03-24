// <copyright file="UncategorizedSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Summary of uncategorized transactions across all accounts.</summary>
public sealed class UncategorizedSummaryDto
{
    /// <summary>Gets or sets the per-account uncategorized summaries.</summary>
    public IReadOnlyList<AccountUncategorizedSummaryDto> ByAccount { get; set; } = [];

    /// <summary>Gets or sets the total number of uncategorized transactions.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the total absolute amount of uncategorized transactions.</summary>
    public decimal TotalAmount { get; set; }
}

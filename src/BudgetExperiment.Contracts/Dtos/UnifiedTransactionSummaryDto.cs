// <copyright file="UnifiedTransactionSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Summary statistics for the unified transaction list result set.
/// </summary>
public sealed class UnifiedTransactionSummaryDto
{
    /// <summary>Gets or sets the total count of matching transactions.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the net total amount of matching transactions.</summary>
    public MoneyDto TotalAmount { get; set; } = new();

    /// <summary>Gets or sets the total income (positive amounts).</summary>
    public MoneyDto IncomeTotal { get; set; } = new();

    /// <summary>Gets or sets the total expenses (negative amounts).</summary>
    public MoneyDto ExpenseTotal { get; set; } = new();

    /// <summary>Gets or sets the count of uncategorized transactions in the result set.</summary>
    public int UncategorizedCount { get; set; }
}

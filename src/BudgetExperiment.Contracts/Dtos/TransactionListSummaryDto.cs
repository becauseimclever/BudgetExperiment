// <copyright file="TransactionListSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Summary DTO for the transaction list view.
/// </summary>
public sealed class TransactionListSummaryDto
{
    /// <summary>Gets or sets the total amount of all items in the list.</summary>
    public MoneyDto TotalAmount { get; set; } = new();

    /// <summary>Gets or sets the total income (positive amounts).</summary>
    public MoneyDto TotalIncome { get; set; } = new();

    /// <summary>Gets or sets the total expenses (negative amounts).</summary>
    public MoneyDto TotalExpenses { get; set; } = new();

    /// <summary>Gets or sets the count of actual transactions.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the count of recurring transaction instances.</summary>
    public int RecurringCount { get; set; }

    /// <summary>Gets or sets the current balance (initial balance + total amount).</summary>
    public MoneyDto CurrentBalance { get; set; } = new();
}

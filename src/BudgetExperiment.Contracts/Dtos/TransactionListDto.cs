// <copyright file="TransactionListDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for the account transaction list view with pre-merged transactions and recurring instances.
/// </summary>
public sealed class TransactionListDto
{
    /// <summary>Gets or sets the account ID.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date of the query range.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the end date of the query range.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Gets or sets the account's initial balance.</summary>
    public MoneyDto InitialBalance { get; set; } = new();

    /// <summary>Gets or sets the account's initial balance date.</summary>
    public DateOnly InitialBalanceDate { get; set; }

    /// <summary>Gets or sets the list of items (transactions and recurring instances, pre-merged).</summary>
    public IReadOnlyList<TransactionListItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the summary for this list.</summary>
    public TransactionListSummaryDto Summary { get; set; } = new();
}

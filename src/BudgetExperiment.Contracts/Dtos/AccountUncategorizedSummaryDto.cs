// <copyright file="AccountUncategorizedSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Summary of uncategorized transactions for a single account.</summary>
public sealed class AccountUncategorizedSummaryDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of uncategorized transactions.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets the total absolute amount of uncategorized transactions.</summary>
    public decimal Amount { get; set; }
}

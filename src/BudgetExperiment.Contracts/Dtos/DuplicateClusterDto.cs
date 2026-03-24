// <copyright file="DuplicateClusterDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>A cluster of duplicate transactions grouped by shared attributes.</summary>
public sealed class DuplicateClusterDto
{
    /// <summary>Gets or sets the group key describing the cluster (date + amount + normalized description).</summary>
    public string GroupKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the transactions in this duplicate cluster.</summary>
    public IReadOnlyList<TransactionDto> Transactions { get; set; } = [];
}

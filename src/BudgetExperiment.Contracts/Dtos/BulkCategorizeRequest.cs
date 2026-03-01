// <copyright file="BulkCategorizeRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to bulk categorize transactions.
/// </summary>
public sealed class BulkCategorizeRequest
{
    /// <summary>Gets or sets the transaction IDs to categorize.</summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];

    /// <summary>Gets or sets the target category ID.</summary>
    public Guid CategoryId { get; set; }
}

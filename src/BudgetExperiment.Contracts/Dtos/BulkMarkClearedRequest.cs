// <copyright file="BulkMarkClearedRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to bulk-mark transactions as cleared.</summary>
public sealed class BulkMarkClearedRequest
{
    /// <summary>Gets or sets the transaction identifiers.</summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];

    /// <summary>Gets or sets the date transactions cleared.</summary>
    public DateOnly ClearedDate { get; set; }
}

// <copyright file="BulkMarkUnclearedRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to bulk-unclear transactions.</summary>
public sealed class BulkMarkUnclearedRequest
{
    /// <summary>Gets or sets the transaction identifiers.</summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];
}

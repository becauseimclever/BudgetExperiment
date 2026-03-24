// <copyright file="MergeDuplicatesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to merge duplicate transactions into a primary transaction.</summary>
public sealed class MergeDuplicatesRequest
{
    /// <summary>Gets or sets the primary transaction identifier to keep.</summary>
    public Guid PrimaryTransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the duplicate transaction identifiers to remove.</summary>
    public IReadOnlyList<Guid> DuplicateIds { get; set; } = [];
}

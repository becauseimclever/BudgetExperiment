// <copyright file="MarkClearedRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to mark a transaction as cleared.</summary>
public sealed class MarkClearedRequest
{
    /// <summary>Gets or sets the transaction identifier.</summary>
    public Guid TransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the date the transaction cleared.</summary>
    public DateOnly ClearedDate
    {
        get; set;
    }
}

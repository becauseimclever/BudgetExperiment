// <copyright file="MarkUnclearedRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to uncleared a transaction.</summary>
public sealed class MarkUnclearedRequest
{
    /// <summary>Gets or sets the transaction identifier.</summary>
    public Guid TransactionId { get; set; }
}

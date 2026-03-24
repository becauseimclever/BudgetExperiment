// <copyright file="CompleteReconciliationRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to complete reconciliation for an account.</summary>
public sealed class CompleteReconciliationRequest
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }
}

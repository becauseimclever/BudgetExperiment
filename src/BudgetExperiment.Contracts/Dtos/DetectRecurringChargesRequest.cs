// <copyright file="DetectRecurringChargesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to trigger recurring charge detection.
/// </summary>
public sealed record DetectRecurringChargesRequest
{
    /// <summary>
    /// Gets the optional account ID to scope detection. Null runs across all accounts.
    /// </summary>
    public Guid? AccountId
    {
        get; init;
    }
}

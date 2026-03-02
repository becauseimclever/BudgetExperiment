// <copyright file="ManualMatchRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to manually match a transaction to a recurring instance.
/// </summary>
public sealed record ManualMatchRequest
{
    /// <summary>
    /// Gets the transaction ID to match.
    /// </summary>
    public Guid TransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction ID.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the instance date to match.
    /// </summary>
    public DateOnly InstanceDate { get; init; }
}

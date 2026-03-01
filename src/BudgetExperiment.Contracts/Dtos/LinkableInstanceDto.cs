// <copyright file="LinkableInstanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a recurring instance that can be linked to a transaction.
/// </summary>
public sealed record LinkableInstanceDto
{
    /// <summary>
    /// Gets the recurring transaction identifier.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected amount for this instance.
    /// </summary>
    public MoneyDto ExpectedAmount { get; init; } = default!;

    /// <summary>
    /// Gets the scheduled date for this instance.
    /// </summary>
    public DateOnly InstanceDate { get; init; }

    /// <summary>
    /// Gets a value indicating whether this instance is already matched to another transaction.
    /// </summary>
    public bool IsAlreadyMatched { get; init; }

    /// <summary>
    /// Gets the confidence score if auto-matched to this transaction (0.0 to 1.0).
    /// </summary>
    public decimal? SuggestedConfidence { get; init; }
}

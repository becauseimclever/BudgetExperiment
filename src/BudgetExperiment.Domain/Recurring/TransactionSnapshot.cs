// <copyright file="TransactionSnapshot.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// A lightweight read-only snapshot of a transaction for use in recurrence detection.
/// Decouples the detection algorithm from the full Transaction entity.
/// </summary>
/// <param name="Id">The transaction identifier.</param>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Description">The raw transaction description.</param>
/// <param name="Amount">The transaction amount (signed decimal).</param>
/// <param name="Currency">The ISO currency code.</param>
/// <param name="Date">The transaction date.</param>
/// <param name="CategoryId">The optional category identifier.</param>
/// <param name="RecurringTransactionId">The optional linked recurring transaction identifier.</param>
public record TransactionSnapshot(
    Guid Id,
    Guid AccountId,
    string Description,
    decimal Amount,
    string Currency,
    DateOnly Date,
    Guid? CategoryId,
    Guid? RecurringTransactionId);

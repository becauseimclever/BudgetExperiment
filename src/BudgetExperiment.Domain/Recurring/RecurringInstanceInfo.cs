// <copyright file="RecurringInstanceInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Represents a projected instance of a recurring transaction for a specific date.
/// </summary>
/// <param name="RecurringTransactionId">The ID of the recurring transaction.</param>
/// <param name="InstanceDate">The date of this instance.</param>
/// <param name="AccountId">The account ID associated with this instance.</param>
/// <param name="AccountName">The account name associated with this instance.</param>
/// <param name="Description">The description (may be modified by exception).</param>
/// <param name="Amount">The amount (may be modified by exception).</param>
/// <param name="CategoryId">The category ID (if any).</param>
/// <param name="CategoryName">The category name (null if uncategorized).</param>
/// <param name="IsModified">Whether this instance has been modified via exception.</param>
/// <param name="IsSkipped">Whether this instance has been skipped via exception.</param>
/// <param name="ImportPatterns">Optional import patterns for pattern-based matching.</param>
public sealed record RecurringInstanceInfo(
    Guid RecurringTransactionId,
    DateOnly InstanceDate,
    Guid AccountId,
    string AccountName,
    string Description,
    MoneyValue Amount,
    Guid? CategoryId,
    string? CategoryName,
    bool IsModified,
    bool IsSkipped,
    IReadOnlyCollection<ImportPattern>? ImportPatterns = null);

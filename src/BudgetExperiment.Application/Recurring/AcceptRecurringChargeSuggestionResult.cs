// <copyright file="AcceptRecurringChargeSuggestionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Result of accepting a recurring charge suggestion.
/// </summary>
/// <param name="RecurringTransactionId">The ID of the newly created recurring transaction.</param>
/// <param name="LinkedTransactionCount">The number of existing transactions linked to the recurring transaction.</param>
public sealed record AcceptRecurringChargeSuggestionResult(
    Guid RecurringTransactionId,
    int LinkedTransactionCount);

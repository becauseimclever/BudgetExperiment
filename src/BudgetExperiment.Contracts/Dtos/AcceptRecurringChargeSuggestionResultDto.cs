// <copyright file="AcceptRecurringChargeSuggestionResultDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result DTO after accepting a recurring charge suggestion.
/// </summary>
public sealed class AcceptRecurringChargeSuggestionResultDto
{
    /// <summary>
    /// Gets or sets the ID of the newly created recurring transaction.
    /// </summary>
    public Guid RecurringTransactionId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of existing transactions linked to the recurring transaction.
    /// </summary>
    public int LinkedTransactionCount
    {
        get; set;
    }
}

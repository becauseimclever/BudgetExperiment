// <copyright file="ChatActionType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines the type of chat action.
/// </summary>
public enum ChatActionType
{
    /// <summary>
    /// Create a new transaction.
    /// </summary>
    CreateTransaction = 0,

    /// <summary>
    /// Create a transfer between accounts.
    /// </summary>
    CreateTransfer = 1,

    /// <summary>
    /// Create a recurring transaction.
    /// </summary>
    CreateRecurringTransaction = 2,

    /// <summary>
    /// Create a recurring transfer.
    /// </summary>
    CreateRecurringTransfer = 3,

    /// <summary>
    /// Clarification is needed from the user.
    /// </summary>
    ClarificationNeeded = 4,
}

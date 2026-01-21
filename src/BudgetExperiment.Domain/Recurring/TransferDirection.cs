// <copyright file="TransferDirection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Indicates the direction of a transfer transaction relative to the account.
/// </summary>
public enum TransferDirection
{
    /// <summary>
    /// Money is leaving this account (the source account of the transfer).
    /// </summary>
    Source = 0,

    /// <summary>
    /// Money is entering this account (the destination account of the transfer).
    /// </summary>
    Destination = 1,
}

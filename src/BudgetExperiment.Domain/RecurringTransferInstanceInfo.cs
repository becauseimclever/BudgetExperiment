// <copyright file="RecurringTransferInstanceInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a projected instance of a recurring transfer for a specific date.
/// </summary>
/// <param name="RecurringTransferId">The ID of the recurring transfer.</param>
/// <param name="InstanceDate">The date of this instance.</param>
/// <param name="AccountId">The account ID associated with this side of the transfer.</param>
/// <param name="AccountName">The account name associated with this side of the transfer.</param>
/// <param name="Description">The description (may be modified by exception).</param>
/// <param name="Amount">The amount (may be modified by exception). Negative for source, positive for destination.</param>
/// <param name="IsModified">Whether this instance has been modified via exception.</param>
/// <param name="IsSkipped">Whether this instance has been skipped via exception.</param>
/// <param name="TransferDirection">The direction of the transfer (Source or Destination).</param>
public sealed record RecurringTransferInstanceInfo(
    Guid RecurringTransferId,
    DateOnly InstanceDate,
    Guid AccountId,
    string AccountName,
    string Description,
    MoneyValue Amount,
    bool IsModified,
    bool IsSkipped,
    string TransferDirection);

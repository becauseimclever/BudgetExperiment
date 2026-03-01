// <copyright file="RecurringTransferInstanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a projected recurring transfer instance.
/// </summary>
public sealed class RecurringTransferInstanceDto
{
    /// <summary>Gets or sets the recurring transfer identifier.</summary>
    public Guid RecurringTransferId { get; set; }

    /// <summary>Gets or sets the scheduled date of this instance.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Gets or sets the effective date (may differ if rescheduled).</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Gets or sets the amount (may be modified from series).</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the description (may be modified from series).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this instance has modifications.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets a value indicating whether this instance is skipped.</summary>
    public bool IsSkipped { get; set; }

    /// <summary>Gets or sets a value indicating whether transactions have been generated for this instance.</summary>
    public bool IsGenerated { get; set; }

    /// <summary>Gets or sets the source transaction ID (null if not yet generated).</summary>
    public Guid? SourceTransactionId { get; set; }

    /// <summary>Gets or sets the destination transaction ID (null if not yet generated).</summary>
    public Guid? DestinationTransactionId { get; set; }
}

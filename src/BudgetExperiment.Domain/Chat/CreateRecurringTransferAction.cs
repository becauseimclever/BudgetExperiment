// <copyright file="CreateRecurringTransferAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Action to create a recurring transfer.
/// </summary>
public sealed record CreateRecurringTransferAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.CreateRecurringTransfer;

    /// <summary>
    /// Gets the source account identifier.
    /// </summary>
    public Guid FromAccountId { get; init; }

    /// <summary>
    /// Gets the source account name for display.
    /// </summary>
    public string FromAccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the destination account identifier.
    /// </summary>
    public Guid ToAccountId { get; init; }

    /// <summary>
    /// Gets the destination account name for display.
    /// </summary>
    public string ToAccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transfer amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the optional transfer description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the recurrence pattern.
    /// </summary>
    public RecurrencePattern Recurrence { get; init; } = null!;

    /// <summary>
    /// Gets the start date for the recurrence.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the optional end date for the recurrence.
    /// </summary>
    public DateOnly? EndDate { get; init; }

    /// <inheritdoc/>
    public override string GetPreviewSummary() =>
        $"Recurring Transfer: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} from {this.FromAccountName} to {this.ToAccountName} ({this.Recurrence})";
}

// <copyright file="CreateRecurringTransactionAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Action to create a recurring transaction.
/// </summary>
public sealed record CreateRecurringTransactionAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.CreateRecurringTransaction;

    /// <summary>
    /// Gets the target account identifier.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the target account name for display.
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional category name.
    /// </summary>
    public string? Category { get; init; }

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
        $"Recurring: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} - {this.Description} ({this.Recurrence})";
}

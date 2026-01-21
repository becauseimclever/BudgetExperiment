// <copyright file="ChatAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Base record for chat actions that can be executed.
/// </summary>
public abstract record ChatAction
{
    /// <summary>
    /// Gets the type of this action.
    /// </summary>
    public abstract ChatActionType Type { get; }

    /// <summary>
    /// Gets a human-readable summary of the action for preview.
    /// </summary>
    /// <returns>A formatted summary string.</returns>
    public abstract string GetPreviewSummary();
}

/// <summary>
/// Action to create a new transaction.
/// </summary>
public sealed record CreateTransactionAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.CreateTransaction;

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
    /// Gets the transaction date.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional category name.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the optional category identifier.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <inheritdoc/>
    public override string GetPreviewSummary() =>
        $"Transaction: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} - {this.Description} on {this.Date:d} ({this.AccountName})";
}

/// <summary>
/// Action to create a transfer between accounts.
/// </summary>
public sealed record CreateTransferAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.CreateTransfer;

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
    /// Gets the transfer date.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the optional transfer description.
    /// </summary>
    public string? Description { get; init; }

    /// <inheritdoc/>
    public override string GetPreviewSummary() =>
        $"Transfer: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} from {this.FromAccountName} to {this.ToAccountName} on {this.Date:d}";
}

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

/// <summary>
/// Action indicating clarification is needed from the user.
/// </summary>
public sealed record ClarificationNeededAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.ClarificationNeeded;

    /// <summary>
    /// Gets the question to ask the user.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the field that needs clarification.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the available options to choose from.
    /// </summary>
    public IReadOnlyList<ClarificationOption> Options { get; init; } = Array.Empty<ClarificationOption>();

    /// <inheritdoc/>
    public override string GetPreviewSummary() => this.Question;
}

/// <summary>
/// An option for clarification selection.
/// </summary>
public sealed record ClarificationOption
{
    /// <summary>
    /// Gets the display label for the option.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the value to use when selected.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional entity identifier (for account/category selection).
    /// </summary>
    public Guid? EntityId { get; init; }
}

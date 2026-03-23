// <copyright file="CreateTransactionAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Chat;

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
    public Guid AccountId
    {
        get; init;
    }

    /// <summary>
    /// Gets the target account name for display.
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public decimal Amount
    {
        get; init;
    }

    /// <summary>
    /// Gets the transaction date.
    /// </summary>
    public DateOnly Date
    {
        get; init;
    }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional category name.
    /// </summary>
    public string? Category
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional category identifier.
    /// </summary>
    public Guid? CategoryId
    {
        get; init;
    }

    /// <inheritdoc/>
    public override string GetPreviewSummary() =>
        $"Transaction: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} - {this.Description} on {this.Date:d} ({this.AccountName})";
}

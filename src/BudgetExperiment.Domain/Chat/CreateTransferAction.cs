// <copyright file="CreateTransferAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Chat;

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
    public Guid FromAccountId
    {
        get; init;
    }

    /// <summary>
    /// Gets the source account name for display.
    /// </summary>
    public string FromAccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the destination account identifier.
    /// </summary>
    public Guid ToAccountId
    {
        get; init;
    }

    /// <summary>
    /// Gets the destination account name for display.
    /// </summary>
    public string ToAccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transfer amount.
    /// </summary>
    public decimal Amount
    {
        get; init;
    }

    /// <summary>
    /// Gets the transfer date.
    /// </summary>
    public DateOnly Date
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional transfer description.
    /// </summary>
    public string? Description
    {
        get; init;
    }

    /// <inheritdoc/>
    public override string GetPreviewSummary() =>
        $"Transfer: {this.Amount.ToString("C", CultureInfo.CurrentCulture)} from {this.FromAccountName} to {this.ToAccountName} on {this.Date:d}";
}

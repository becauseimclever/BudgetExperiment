// <copyright file="TransferModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for transfer data.
/// </summary>
public sealed class TransferModel
{
    /// <summary>Gets or sets the transfer identifier.</summary>
    public Guid TransferId { get; set; }

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer amount (always positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the source transaction identifier.</summary>
    public Guid SourceTransactionId { get; set; }

    /// <summary>Gets or sets the destination transaction identifier.</summary>
    public Guid DestinationTransactionId { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Client-side model for transfer list items.
/// </summary>
public sealed class TransferListItemModel
{
    /// <summary>Gets or sets the transfer identifier.</summary>
    public Guid TransferId { get; set; }

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer amount (always positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Client-side model for creating a transfer.
/// </summary>
public sealed class TransferCreateModel
{
    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the transfer amount (must be positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Client-side model for updating a transfer.
/// </summary>
public sealed class TransferUpdateModel
{
    /// <summary>Gets or sets the transfer amount (must be positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}

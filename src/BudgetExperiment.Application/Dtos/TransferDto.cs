// <copyright file="TransferDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Dtos;

/// <summary>
/// DTO for creating a transfer between accounts.
/// </summary>
public sealed class CreateTransferRequest
{
    /// <summary>Gets or sets the source account identifier (money leaving).</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the destination account identifier (money entering).</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the transfer amount (must be positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code (e.g., "USD").</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing transfer.
/// </summary>
public sealed class UpdateTransferRequest
{
    /// <summary>Gets or sets the transfer amount (must be positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code (e.g., "USD").</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO for returning full transfer details.
/// </summary>
public sealed class TransferResponse
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
/// DTO for transfer list items (lighter weight than full response).
/// </summary>
public sealed class TransferListItemResponse
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

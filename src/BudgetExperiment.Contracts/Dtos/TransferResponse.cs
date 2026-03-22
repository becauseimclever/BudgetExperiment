// <copyright file="TransferResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning full transfer details.
/// </summary>
public sealed class TransferResponse
{
    /// <summary>Gets or sets the transfer identifier.</summary>
    public Guid TransferId
    {
        get; set;
    }

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer amount (always positive).</summary>
    public decimal Amount
    {
        get; set;
    }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date
    {
        get; set;
    }

    /// <summary>Gets or sets the description.</summary>
    public string? Description
    {
        get; set;
    }

    /// <summary>Gets or sets the source transaction identifier.</summary>
    public Guid SourceTransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the destination transaction identifier.</summary>
    public Guid DestinationTransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc
    {
        get; set;
    }
}

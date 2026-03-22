// <copyright file="TransferListItemResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for transfer list items (lighter weight than full response).
/// </summary>
public sealed class TransferListItemResponse
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
}

// <copyright file="CreateTransferRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for creating a transfer between accounts.
/// </summary>
public sealed class CreateTransferRequest
{
    /// <summary>Gets or sets the source account identifier (money leaving).</summary>
    public Guid SourceAccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the destination account identifier (money entering).</summary>
    public Guid DestinationAccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the transfer amount (must be positive).</summary>
    public decimal Amount
    {
        get; set;
    }

    /// <summary>Gets or sets the currency code (e.g., "USD").</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transfer date.</summary>
    public DateOnly Date
    {
        get; set;
    }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description
    {
        get; set;
    }
}

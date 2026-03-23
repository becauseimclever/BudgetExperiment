// <copyright file="UpdateTransferRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating an existing transfer.
/// </summary>
public sealed class UpdateTransferRequest
{
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

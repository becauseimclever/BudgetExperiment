// <copyright file="TransactionUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating an existing transaction.
/// </summary>
public sealed class TransactionUpdateDto
{
    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date
    {
        get; set;
    }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets the per-transaction Kakeibo override (enum name, e.g. "Essentials", "Wants", "Culture", "Unexpected"). Null means defer to category routing.</summary>
    public string? KakeiboOverride { get; set; }
}

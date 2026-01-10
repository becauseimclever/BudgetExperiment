// <copyright file="TransactionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Dtos;

/// <summary>
/// DTO for returning transaction details.
/// </summary>
public sealed class TransactionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new transaction.
/// </summary>
public sealed class TransactionCreateDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
}

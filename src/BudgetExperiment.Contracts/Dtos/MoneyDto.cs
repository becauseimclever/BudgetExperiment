// <copyright file="MoneyDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for monetary values.
/// </summary>
public sealed class MoneyDto
{
    /// <summary>Gets or sets the currency code (ISO 4217).</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
}

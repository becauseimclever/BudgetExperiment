// <copyright file="RealizeRecurringTransactionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for realizing a recurring transaction instance.
/// </summary>
public sealed class RealizeRecurringTransactionRequest
{
    /// <summary>Gets or sets the scheduled instance date to realize.</summary>
    public DateOnly InstanceDate { get; set; }

    /// <summary>Gets or sets the optional actual date (defaults to InstanceDate if not provided).</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Gets or sets the optional override amount.</summary>
    public MoneyDto? Amount { get; set; }

    /// <summary>Gets or sets the optional override description.</summary>
    public string? Description { get; set; }
}

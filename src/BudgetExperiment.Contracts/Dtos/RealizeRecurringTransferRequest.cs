// <copyright file="RealizeRecurringTransferRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for realizing a recurring transfer instance.
/// </summary>
public sealed class RealizeRecurringTransferRequest
{
    /// <summary>Gets or sets the scheduled instance date to realize.</summary>
    public DateOnly InstanceDate { get; set; }

    /// <summary>Gets or sets the optional actual date (defaults to InstanceDate if not provided).</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Gets or sets the optional override amount.</summary>
    public MoneyDto? Amount { get; set; }
}

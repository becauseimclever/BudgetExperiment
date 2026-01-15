// <copyright file="PaycheckAllocationWarningDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a paycheck allocation warning.
/// </summary>
public sealed class PaycheckAllocationWarningDto
{
    /// <summary>Gets or sets the warning type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the warning message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional amount (e.g., shortfall).</summary>
    public MoneyDto? Amount { get; set; }
}

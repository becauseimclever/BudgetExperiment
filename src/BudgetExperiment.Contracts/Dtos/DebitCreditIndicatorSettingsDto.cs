// <copyright file="DebitCreditIndicatorSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for debit/credit indicator settings.
/// </summary>
public sealed record DebitCreditIndicatorSettingsDto
{
    /// <summary>
    /// Gets the column index of the indicator (-1 if disabled).
    /// </summary>
    public int ColumnIndex { get; init; } = -1;

    /// <summary>
    /// Gets the comma-separated debit indicator values.
    /// </summary>
    public string DebitIndicators { get; init; } = string.Empty;

    /// <summary>
    /// Gets the comma-separated credit indicator values.
    /// </summary>
    public string CreditIndicators { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }
}

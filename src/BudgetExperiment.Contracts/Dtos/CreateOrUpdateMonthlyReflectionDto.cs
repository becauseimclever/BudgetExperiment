// <copyright file="CreateOrUpdateMonthlyReflectionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for creating or updating a monthly Kakeibo reflection.
/// </summary>
public sealed class CreateOrUpdateMonthlyReflectionDto
{
    /// <summary>
    /// Gets or sets the savings goal for the month (must be non-negative).
    /// </summary>
    public decimal SavingsGoal
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-start intention text (max 280 characters).
    /// </summary>
    public string? IntentionText
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-end gratitude journal entry (max 2000 characters).
    /// </summary>
    public string? GratitudeText
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-end improvement journal entry (max 2000 characters).
    /// </summary>
    public string? ImprovementText
    {
        get; set;
    }
}

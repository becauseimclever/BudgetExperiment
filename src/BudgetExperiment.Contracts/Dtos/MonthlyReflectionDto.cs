// <copyright file="MonthlyReflectionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO for a monthly Kakeibo reflection.
/// </summary>
public sealed class MonthlyReflectionDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the reflection.
    /// </summary>
    public Guid Id
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the year this reflection covers.
    /// </summary>
    public int Year
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month (1-12) this reflection covers.
    /// </summary>
    public int Month
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the savings goal the user set at month start.
    /// </summary>
    public decimal SavingsGoal
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the actual savings recorded at month end (income minus expenses).
    /// </summary>
    public decimal? ActualSavings
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-start intention text.
    /// </summary>
    public string? IntentionText
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-end gratitude journal entry.
    /// </summary>
    public string? GratitudeText
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month-end improvement journal entry.
    /// </summary>
    public string? ImprovementText
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the UTC timestamp when the reflection was created.
    /// </summary>
    public DateTime CreatedAtUtc
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the UTC timestamp when the reflection was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc
    {
        get; set;
    }
}

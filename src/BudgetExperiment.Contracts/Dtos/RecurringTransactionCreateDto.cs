// <copyright file="RecurringTransactionCreateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for creating a new recurring transaction.
/// </summary>
public sealed class RecurringTransactionCreateDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets the recurrence frequency (Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly).</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences (default 1).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Gets or sets the day of month (1-31) for monthly/quarterly/yearly patterns.</summary>
    public int? DayOfMonth
    {
        get; set;
    }

    /// <summary>Gets or sets the day of week for weekly/biweekly patterns.</summary>
    public string? DayOfWeek
    {
        get; set;
    }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear
    {
        get; set;
    }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate
    {
        get; set;
    }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate
    {
        get; set;
    }
}

// <copyright file="RecurringTransactionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning recurring transaction details.
/// </summary>
public sealed class RecurringTransactionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences.</summary>
    public int Interval { get; set; }

    /// <summary>Gets or sets the day of month (1-31) for monthly patterns.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week for weekly patterns.</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear { get; set; }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the next occurrence date.</summary>
    public DateOnly NextOccurrence { get; set; }

    /// <summary>Gets or sets a value indicating whether the recurring transaction is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// DTO for creating a new recurring transaction.
/// </summary>
public sealed class RecurringTransactionCreateDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the recurrence frequency (Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly).</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences (default 1).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Gets or sets the day of month (1-31) for monthly/quarterly/yearly patterns.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week for weekly/biweekly patterns.</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear { get; set; }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }
}

/// <summary>
/// DTO for updating a recurring transaction.
/// </summary>
public sealed class RecurringTransactionUpdateDto
{
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the interval between occurrences.</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Gets or sets the day of month (1-31) for monthly patterns.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week for weekly patterns.</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the month of year (1-12) for yearly patterns.</summary>
    public int? MonthOfYear { get; set; }

    /// <summary>Gets or sets the optional end date (null to remove).</summary>
    public DateOnly? EndDate { get; set; }
}

/// <summary>
/// DTO for a projected recurring transaction instance.
/// </summary>
public sealed class RecurringInstanceDto
{
    /// <summary>Gets or sets the recurring transaction identifier.</summary>
    public Guid RecurringTransactionId { get; set; }

    /// <summary>Gets or sets the scheduled date of this instance.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Gets or sets the effective date (may differ if rescheduled).</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Gets or sets the amount (may be modified from series).</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the description (may be modified from series).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this instance has modifications.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets a value indicating whether this instance is skipped.</summary>
    public bool IsSkipped { get; set; }

    /// <summary>Gets or sets a value indicating whether a transaction has been generated for this instance.</summary>
    public bool IsGenerated { get; set; }

    /// <summary>Gets or sets the generated transaction ID (null if not yet generated).</summary>
    public Guid? GeneratedTransactionId { get; set; }

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }
}

/// <summary>
/// DTO for modifying a single instance of a recurring transaction.
/// </summary>
public sealed class RecurringInstanceModifyDto
{
    /// <summary>Gets or sets the modified amount (null = use series amount).</summary>
    public MoneyDto? Amount { get; set; }

    /// <summary>Gets or sets the modified description (null = use series description).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the modified date for rescheduling (null = use original date).</summary>
    public DateOnly? Date { get; set; }
}

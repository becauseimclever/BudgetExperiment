// <copyright file="RecurringTransferDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning recurring transfer details.
/// </summary>
public sealed class RecurringTransferDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount (always positive).</summary>
    public MoneyDto Amount { get; set; } = new();

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

    /// <summary>Gets or sets a value indicating whether the recurring transfer is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// DTO for creating a new recurring transfer.
/// </summary>
public sealed class RecurringTransferCreateDto
{
    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount (must be positive).</summary>
    public MoneyDto Amount { get; set; } = new();

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
/// DTO for updating a recurring transfer.
/// </summary>
public sealed class RecurringTransferUpdateDto
{
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount (must be positive).</summary>
    public MoneyDto Amount { get; set; } = new();

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
/// DTO for a projected recurring transfer instance.
/// </summary>
public sealed class RecurringTransferInstanceDto
{
    /// <summary>Gets or sets the recurring transfer identifier.</summary>
    public Guid RecurringTransferId { get; set; }

    /// <summary>Gets or sets the scheduled date of this instance.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Gets or sets the effective date (may differ if rescheduled).</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Gets or sets the amount (may be modified from series).</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the description (may be modified from series).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the source account identifier.</summary>
    public Guid SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name.</summary>
    public string SourceAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination account identifier.</summary>
    public Guid DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name.</summary>
    public string DestinationAccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this instance has modifications.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets a value indicating whether this instance is skipped.</summary>
    public bool IsSkipped { get; set; }

    /// <summary>Gets or sets a value indicating whether transactions have been generated for this instance.</summary>
    public bool IsGenerated { get; set; }

    /// <summary>Gets or sets the source transaction ID (null if not yet generated).</summary>
    public Guid? SourceTransactionId { get; set; }

    /// <summary>Gets or sets the destination transaction ID (null if not yet generated).</summary>
    public Guid? DestinationTransactionId { get; set; }
}

/// <summary>
/// DTO for modifying a single instance of a recurring transfer.
/// </summary>
public sealed class RecurringTransferInstanceModifyDto
{
    /// <summary>Gets or sets the modified amount (null = use series amount).</summary>
    public MoneyDto? Amount { get; set; }

    /// <summary>Gets or sets the modified description (null = use series description).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the modified date for rescheduling (null = use original date).</summary>
    public DateOnly? Date { get; set; }
}

// <copyright file="RecurringTransactionModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for recurring transaction data.
/// </summary>
public sealed class RecurringTransactionModel
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyModel Amount { get; set; } = new();

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = "Monthly";

    /// <summary>Gets or sets the day of month (for monthly frequency).</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week (for weekly frequency).</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the next occurrence date.</summary>
    public DateOnly NextOccurrence { get; set; }

    /// <summary>Gets or sets whether the recurring transaction is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Client-side model for creating a new recurring transaction.
/// </summary>
public sealed class RecurringTransactionCreateModel
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the recurrence frequency.</summary>
    public string Frequency { get; set; } = "Monthly";

    /// <summary>Gets or sets the day of month (for monthly frequency).</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Gets or sets the day of week (for weekly frequency).</summary>
    public string? DayOfWeek { get; set; }

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
}

/// <summary>
/// Client-side model for updating a recurring transaction.
/// </summary>
public sealed class RecurringTransactionUpdateModel
{
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string? Currency { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
}

/// <summary>
/// Client-side model for a recurring transaction instance (projected or with exception).
/// </summary>
public sealed class RecurringInstanceModel
{
    /// <summary>Gets or sets the recurring transaction identifier.</summary>
    public Guid RecurringTransactionId { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled date.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Gets or sets the amount.</summary>
    public MoneyModel Amount { get; set; } = new();

    /// <summary>Gets or sets whether this instance is modified.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets whether this instance is skipped.</summary>
    public bool IsSkipped { get; set; }

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
}

/// <summary>
/// Client-side model for modifying a single recurring transaction instance.
/// </summary>
public sealed class RecurringInstanceModifyModel
{
    /// <summary>Gets or sets the optional new date for this instance.</summary>
    public DateOnly? NewDate { get; set; }

    /// <summary>Gets or sets the optional new amount.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Gets or sets the optional currency code.</summary>
    public string? Currency { get; set; }

    /// <summary>Gets or sets the optional new description.</summary>
    public string? Description { get; set; }
}

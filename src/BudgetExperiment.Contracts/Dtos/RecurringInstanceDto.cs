// <copyright file="RecurringInstanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a projected recurring transaction instance.
/// </summary>
public sealed class RecurringInstanceDto
{
    /// <summary>Gets or sets the recurring transaction identifier.</summary>
    public Guid RecurringTransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the scheduled date of this instance.</summary>
    public DateOnly ScheduledDate
    {
        get; set;
    }

    /// <summary>Gets or sets the effective date (may differ if rescheduled).</summary>
    public DateOnly EffectiveDate
    {
        get; set;
    }

    /// <summary>Gets or sets the amount (may be modified from series).</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the description (may be modified from series).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this instance has modifications.</summary>
    public bool IsModified
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether this instance is skipped.</summary>
    public bool IsSkipped
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether a transaction has been generated for this instance.</summary>
    public bool IsGenerated
    {
        get; set;
    }

    /// <summary>Gets or sets the generated transaction ID (null if not yet generated).</summary>
    public Guid? GeneratedTransactionId
    {
        get; set;
    }

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets the category name (null if uncategorized).</summary>
    public string? CategoryName
    {
        get; set;
    }
}

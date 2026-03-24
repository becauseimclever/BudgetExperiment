// <copyright file="DateGapDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Represents a gap in transaction dates for an account.</summary>
public sealed class DateGapDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date of the gap (day after last transaction).</summary>
    public DateOnly GapStart { get; set; }

    /// <summary>Gets or sets the end date of the gap (day before next transaction).</summary>
    public DateOnly GapEnd { get; set; }

    /// <summary>Gets or sets the duration of the gap in days.</summary>
    public int DurationDays { get; set; }
}

// <copyright file="DailyTotalModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for daily transaction totals in calendar view.
/// </summary>
public sealed class DailyTotalModel
{
    /// <summary>Gets or sets the date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the total amount for the day.</summary>
    public MoneyModel Total { get; set; } = new();

    /// <summary>Gets or sets the number of transactions on this day.</summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// Client-side model for monetary values (simple DTO for JSON deserialization).
/// </summary>
public sealed class MoneyModel
{
    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
}

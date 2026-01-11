// -----------------------------------------------------------------------
// <copyright file="CalendarDayModel.cs" company="BudgetExperiment">
//     Copyright (c) BudgetExperiment. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents a day in the calendar grid with its display state.
/// </summary>
public class CalendarDayModel
{
    /// <summary>
    /// Gets or sets the date for this calendar day.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this day is in the currently displayed month.
    /// </summary>
    public bool IsCurrentMonth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this day is today.
    /// </summary>
    public bool IsToday { get; set; }

    /// <summary>
    /// Gets or sets the daily total for this day, if any transactions exist.
    /// </summary>
    public DailyTotalModel? DailyTotal { get; set; }

    /// <summary>
    /// Gets or sets the projected recurring transaction instances for this day.
    /// </summary>
    public List<RecurringInstanceModel> RecurringInstances { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether this day has any projected recurring transactions.
    /// </summary>
    public bool HasRecurring => RecurringInstances.Count > 0;

    /// <summary>
    /// Gets the total projected recurring amount for this day.
    /// </summary>
    public decimal RecurringTotal => RecurringInstances
        .Where(r => !r.IsSkipped)
        .Sum(r => r.Amount.Amount);
}

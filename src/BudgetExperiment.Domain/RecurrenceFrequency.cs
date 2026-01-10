// <copyright file="RecurrenceFrequency.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines the frequency of a recurring transaction.
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// Occurs every day.
    /// </summary>
    Daily = 0,

    /// <summary>
    /// Occurs every week.
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// Occurs every two weeks.
    /// </summary>
    BiWeekly = 2,

    /// <summary>
    /// Occurs every month.
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Occurs every quarter (3 months).
    /// </summary>
    Quarterly = 4,

    /// <summary>
    /// Occurs every year.
    /// </summary>
    Yearly = 5,
}

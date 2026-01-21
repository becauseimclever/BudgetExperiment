// <copyright file="AmountParseMode.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Specifies how amounts should be interpreted during CSV import.
/// </summary>
public enum AmountParseMode
{
    /// <summary>
    /// Negative values are expenses, positive values are income (default bank export behavior).
    /// </summary>
    NegativeIsExpense = 0,

    /// <summary>
    /// Positive values are expenses, negative values are income (inverted convention).
    /// </summary>
    PositiveIsExpense = 1,

    /// <summary>
    /// Separate debit and credit columns are used for expenses and income.
    /// </summary>
    SeparateColumns = 2,

    /// <summary>
    /// Treat all values as expenses (make negative regardless of sign).
    /// </summary>
    AbsoluteExpense = 3,

    /// <summary>
    /// Treat all values as income (make positive regardless of sign).
    /// </summary>
    AbsoluteIncome = 4,

    /// <summary>
    /// Use a separate indicator column to determine if amount is debit or credit.
    /// Amount column contains absolute values; sign determined by indicator.
    /// </summary>
    IndicatorColumn = 5,
}

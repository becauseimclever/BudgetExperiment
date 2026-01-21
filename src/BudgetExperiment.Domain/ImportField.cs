// <copyright file="ImportField.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Specifies the target field for a CSV column mapping.
/// </summary>
public enum ImportField
{
    /// <summary>
    /// Ignore this column during import.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Map to transaction date.
    /// </summary>
    Date = 1,

    /// <summary>
    /// Map to transaction description.
    /// </summary>
    Description = 2,

    /// <summary>
    /// Map to transaction amount (single column for both income and expense).
    /// </summary>
    Amount = 3,

    /// <summary>
    /// Map to debit/expense amount (when using separate columns).
    /// </summary>
    DebitAmount = 4,

    /// <summary>
    /// Map to credit/income amount (when using separate columns).
    /// </summary>
    CreditAmount = 5,

    /// <summary>
    /// Map to category name.
    /// </summary>
    Category = 6,

    /// <summary>
    /// Map to external reference/ID.
    /// </summary>
    Reference = 7,

    /// <summary>
    /// Column indicating whether transaction is debit or credit.
    /// </summary>
    DebitCreditIndicator = 8,
}

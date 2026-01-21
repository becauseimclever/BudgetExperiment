// <copyright file="CategorySource.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Indicates the source of a category assignment during import.
/// </summary>
public enum CategorySource
{
    /// <summary>
    /// Transaction is uncategorized.
    /// </summary>
    None = 0,

    /// <summary>
    /// Category was explicitly provided in the CSV file.
    /// </summary>
    CsvColumn = 1,

    /// <summary>
    /// Category was assigned by an auto-categorization rule.
    /// </summary>
    AutoRule = 2,

    /// <summary>
    /// Category was manually set by user during preview.
    /// </summary>
    UserOverride = 3,
}

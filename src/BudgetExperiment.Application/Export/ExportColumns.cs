// <copyright file="ExportColumns.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Export column header constants used across report export table builders.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class ExportColumns
{
    /// <summary>Category column header, shared across category and budget tables.</summary>
    public const string Category = "Category";

    /// <summary>Transaction count column header, shared across all tables.</summary>
    public const string Transactions = "Transactions";

    /// <summary>Amount column header for category tables.</summary>
    public const string Amount = "Amount";

    /// <summary>Currency column header for category tables.</summary>
    public const string Currency = "Currency";

    /// <summary>Percentage column header for category tables.</summary>
    public const string Percentage = "Percentage";

    /// <summary>Month column header for trends tables.</summary>
    public const string Month = "Month";

    /// <summary>Income column header for trends tables.</summary>
    public const string Income = "Income";

    /// <summary>Spending column header for trends tables.</summary>
    public const string Spending = "Spending";

    /// <summary>Net column header for trends tables.</summary>
    public const string Net = "Net";

    /// <summary>Budgeted amount column header for budget comparison tables.</summary>
    public const string Budgeted = "Budgeted";

    /// <summary>Spent amount column header for budget comparison tables.</summary>
    public const string Spent = "Spent";

    /// <summary>Remaining amount column header for budget comparison tables.</summary>
    public const string Remaining = "Remaining";

    /// <summary>Percent used column header for budget comparison tables.</summary>
    public const string PercentUsed = "PercentUsed";

    /// <summary>Status column header for budget comparison tables.</summary>
    public const string Status = "Status";
}

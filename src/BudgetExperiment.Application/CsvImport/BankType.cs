// <copyright file="BankType.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.CsvImport;

/// <summary>
/// Supported bank types for CSV import.
/// </summary>
public enum BankType
{
    /// <summary>Bank of America.</summary>
    BankOfAmerica = 0,

    /// <summary>Capital One.</summary>
    CapitalOne = 1,

    /// <summary>United Heritage Credit Union.</summary>
    UnitedHeritageCreditUnion = 2,
}

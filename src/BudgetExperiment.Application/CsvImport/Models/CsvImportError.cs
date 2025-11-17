// <copyright file="CsvImportError.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.CsvImport.Models;

/// <summary>
/// Represents an error encountered during CSV import.
/// </summary>
/// <param name="RowNumber">The row number where the error occurred (1-based, excluding header).</param>
/// <param name="Field">The field name that caused the error.</param>
/// <param name="ErrorMessage">Description of the error.</param>
public sealed record CsvImportError(
    int RowNumber,
    string Field,
    string ErrorMessage);

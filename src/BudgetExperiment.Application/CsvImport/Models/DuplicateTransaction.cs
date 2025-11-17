// <copyright file="DuplicateTransaction.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.CsvImport.Models;

/// <summary>
/// Represents a duplicate transaction that was skipped during import.
/// </summary>
/// <param name="RowNumber">The row number in the CSV file (1-based, excluding header).</param>
/// <param name="Date">The transaction date.</param>
/// <param name="Description">The transaction description.</param>
/// <param name="Amount">The transaction amount (absolute value).</param>
/// <param name="ExistingTransactionId">The ID of the existing matching transaction in the database.</param>
public sealed record DuplicateTransaction(
    int RowNumber,
    DateOnly Date,
    string Description,
    decimal Amount,
    Guid ExistingTransactionId);

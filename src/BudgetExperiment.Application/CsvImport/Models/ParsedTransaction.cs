// <copyright file="ParsedTransaction.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.CsvImport.Models;

/// <summary>
/// Represents a transaction parsed from a CSV file.
/// </summary>
/// <param name="Date">Transaction date.</param>
/// <param name="Description">Transaction description.</param>
/// <param name="Amount">Transaction amount (positive for income, negative for expenses).</param>
/// <param name="TransactionType">Type of transaction (Income or Expense).</param>
/// <param name="Category">Optional category.</param>
public sealed record ParsedTransaction(
    DateOnly Date,
    string Description,
    decimal Amount,
    TransactionType TransactionType,
    string? Category);

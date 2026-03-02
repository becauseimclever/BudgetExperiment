// <copyright file="IImportDuplicateDetector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Detects duplicate transactions during CSV import.
/// </summary>
public interface IImportDuplicateDetector
{
    /// <summary>
    /// Checks whether a transaction is a duplicate of an existing one.
    /// </summary>
    /// <param name="date">The transaction date.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="existingTransactions">Existing transactions to check against.</param>
    /// <param name="settings">Duplicate detection settings.</param>
    /// <returns>The ID of the duplicate transaction, or <c>null</c> if no duplicate found.</returns>
    Guid? FindDuplicate(
        DateOnly date,
        decimal amount,
        string description,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto settings);

    /// <summary>
    /// Calculates similarity between two strings using Levenshtein distance.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>A value between 0.0 (no similarity) and 1.0 (identical).</returns>
    double CalculateSimilarity(string a, string b);
}

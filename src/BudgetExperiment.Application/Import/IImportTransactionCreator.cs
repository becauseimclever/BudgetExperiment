// <copyright file="IImportTransactionCreator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Creates transactions from import data within a batch.
/// </summary>
public interface IImportTransactionCreator
{
    /// <summary>
    /// Creates transactions from import data and returns aggregate statistics.
    /// </summary>
    /// <param name="account">The target account.</param>
    /// <param name="batchId">The import batch ID.</param>
    /// <param name="transactions">Transaction data to import.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import transaction result with created IDs and statistics.</returns>
    Task<ImportTransactionResult> CreateTransactionsAsync(
        Account account,
        Guid batchId,
        IReadOnlyList<ImportTransactionData> transactions,
        CancellationToken cancellationToken = default);
}

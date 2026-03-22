// <copyright file="ImportExecuteRequestValidator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Validates <see cref="ImportExecuteRequest"/> before execution.
/// Returns a structured result so the API layer can choose the appropriate HTTP status code.
/// </summary>
public static class ImportExecuteRequestValidator
{
    /// <summary>
    /// Validates an import execute request against size, length, date, and amount constraints.
    /// </summary>
    /// <param name="request">The import execute request to validate.</param>
    /// <returns>A validation result indicating success or a list of errors.</returns>
    public static ImportValidationResult Validate(ImportExecuteRequest request)
    {
        if (request.Transactions.Count == 0)
        {
            return new ImportValidationResult(["No transactions to import."], true);
        }

        if (request.Transactions.Count > ImportValidationConstants.MaxTransactionsPerImport)
        {
            return new ImportValidationResult(
                [$"Import exceeds maximum of {ImportValidationConstants.MaxTransactionsPerImport} transactions."],
                true);
        }

        var errors = new List<string>();
        var maxFutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays);

        for (int i = 0; i < request.Transactions.Count; i++)
        {
            ValidateTransactionRow(request.Transactions[i], i + 1, maxFutureDate, errors);
        }

        return new ImportValidationResult(errors, false);
    }

    private static void ValidateTransactionRow(
        ImportTransactionData tx,
        int rowNumber,
        DateOnly maxFutureDate,
        List<string> errors)
    {
        if (tx.Description.Length > ImportValidationConstants.MaxDescriptionLength)
        {
            errors.Add($"Description exceeds {ImportValidationConstants.MaxDescriptionLength} characters at row {rowNumber}.");
        }

        if (tx.Date > maxFutureDate)
        {
            errors.Add($"Date is too far in the future at row {rowNumber}.");
        }

        if (Math.Abs(tx.Amount) > ImportValidationConstants.MaxAmountAbsoluteValue)
        {
            errors.Add($"Amount out of range at row {rowNumber}.");
        }
    }
}

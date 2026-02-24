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
        var errors = new List<string>();
        bool hasBadRequestError = false;

        // Collection-level checks (400 Bad Request)
        if (request.Transactions.Count == 0)
        {
            errors.Add("No transactions to import.");
            hasBadRequestError = true;
        }
        else if (request.Transactions.Count > ImportValidationConstants.MaxTransactionsPerImport)
        {
            errors.Add($"Import exceeds maximum of {ImportValidationConstants.MaxTransactionsPerImport} transactions.");
            hasBadRequestError = true;
        }

        // Field-level checks (422 Unprocessable Entity) — only if we have transactions
        if (request.Transactions.Count > 0 && request.Transactions.Count <= ImportValidationConstants.MaxTransactionsPerImport)
        {
            var maxFutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays);

            for (int i = 0; i < request.Transactions.Count; i++)
            {
                var tx = request.Transactions[i];
                int rowNumber = i + 1;

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

        return new ImportValidationResult(errors, hasBadRequestError);
    }
}

/// <summary>
/// Result of import request validation.
/// </summary>
/// <param name="Errors">The list of validation error messages.</param>
/// <param name="IsBadRequest">
/// When <c>true</c>, the errors are collection-level (400 Bad Request).
/// When <c>false</c> and errors exist, they are field-level (422 Unprocessable Entity).
/// </param>
public sealed record ImportValidationResult(IReadOnlyList<string> Errors, bool IsBadRequest)
{
    /// <summary>
    /// Gets a value indicating whether the request is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}

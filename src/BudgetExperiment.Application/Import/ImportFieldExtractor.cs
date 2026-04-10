// <copyright file="ImportFieldExtractor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Extracts raw field values from a CSV row using column-to-field mappings.
/// </summary>
internal static class ImportFieldExtractor
{
    /// <summary>
    /// Maps each column in a row to its target import field and returns the extracted values.
    /// </summary>
    /// <param name="row">The raw column values from the CSV row.</param>
    /// <param name="mappings">Column-to-field mappings.</param>
    /// <returns>Extracted field values as a value object.</returns>
    internal static ExtractedColumnValues Extract(IReadOnlyList<string> row, IReadOnlyList<ColumnMappingDto> mappings)
    {
        string? dateStr = null;
        string? description = null;
        string? amountStr = null;
        string? debitStr = null;
        string? creditStr = null;
        string? categoryName = null;
        string? reference = null;
        string? indicatorValue = null;

        foreach (var mapping in mappings)
        {
            if (mapping.ColumnIndex < 0 || mapping.ColumnIndex >= row.Count)
            {
                continue;
            }

            var value = row[mapping.ColumnIndex]?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            switch (mapping.TargetField)
            {
                case ImportField.Date:
                    dateStr = value;
                    break;
                case ImportField.Description:
                    description = string.IsNullOrEmpty(description) ? value : $"{description} {value}";
                    break;
                case ImportField.Amount:
                    amountStr = value;
                    break;
                case ImportField.DebitAmount:
                    debitStr = value;
                    break;
                case ImportField.CreditAmount:
                    creditStr = value;
                    break;
                case ImportField.Category:
                    categoryName = value;
                    break;
                case ImportField.Reference:
                    reference = value;
                    break;
                case ImportField.DebitCreditIndicator:
                    indicatorValue = value;
                    break;
            }
        }

        return new ExtractedColumnValues(dateStr, description, amountStr, debitStr, creditStr, categoryName, reference, indicatorValue);
    }
}

// <copyright file="BankOfAmericaCsvParser.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Domain;

using CsvHelper;
using CsvHelper.Configuration;

namespace BudgetExperiment.Application.CsvImport.Parsers;

/// <summary>
/// CSV parser for Bank of America transaction exports.
/// </summary>
public sealed class BankOfAmericaCsvParser : IBankCsvParser
{
    /// <inheritdoc />
    public BankType BankType => BankType.BankOfAmerica;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        var transactions = new List<ParsedTransaction>();

        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var csv = new CsvReader(reader, config);

        // Skip summary section - BofA CSVs have a summary header before the actual data
        // We need to find the line with "Date,Description,Amount,Running Bal."
        await SkipToDataHeaderAsync(csv, cancellationToken).ConfigureAwait(false);

        // Now read the actual transaction records
        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var dateString = csv.GetField<string>(0)?.Trim();
                var description = csv.GetField<string>(1)?.Trim();
                var amountString = csv.GetField<string>(2)?.Trim();

                // Skip rows with empty amounts (like "Beginning balance" rows)
                if (string.IsNullOrWhiteSpace(amountString))
                {
                    continue;
                }

                // Parse date
                if (!DateOnly.TryParseExact(dateString, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    throw new DomainException($"Invalid date format: '{dateString}'. Expected MM/dd/yyyy format.");
                }

                // Parse amount (remove quotes if present)
                var cleanAmount = amountString?.Replace("\"", string.Empty, StringComparison.Ordinal) ?? string.Empty;
                if (!decimal.TryParse(cleanAmount, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var amount))
                {
                    throw new DomainException($"Invalid amount format: '{amountString}'.");
                }

                // Determine transaction type based on amount sign
                var transactionType = amount >= 0 ? TransactionType.Income : TransactionType.Expense;

                transactions.Add(new ParsedTransaction(
                    Date: date,
                    Description: description ?? string.Empty,
                    Amount: amount,
                    TransactionType: transactionType,
                    Category: null));
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DomainException($"Error parsing CSV row: {ex.Message}");
            }
        }

        return transactions;
    }

    private static async Task SkipToDataHeaderAsync(CsvReader csv, CancellationToken cancellationToken)
    {
        // BofA CSVs have a summary section at the top with headers like "Description,,Summary Amt."
        // We need to skip until we find the actual data header: "Date,Description,Amount,Running Bal."
        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstField = csv.GetField<string>(0)?.Trim();
            if (string.Equals(firstField, "Date", StringComparison.OrdinalIgnoreCase))
            {
                // Found the data header, move to next row to start reading data
                return;
            }
        }

        // If we get here, we didn't find the data header - treat as empty file
    }
}
